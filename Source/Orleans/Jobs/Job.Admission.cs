// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    IGrainTimer? _stageAdmissionTimer;

    enum Admission
    {
        Admitted = 0,
        Refused = 1,
        Failed = 2
    }

    /// <summary>
    /// Gets whether the job is waiting to ask about a stage it could not get an answer for.
    /// </summary>
    bool IsWaitingToAdmitNextStage =>
        _stageAdmissionTimer is not null &&
        State.Status is JobStatus.Running &&
        State.Progress.Stages.Current() is { IsStarted: false };

    static async Task<bool> WaitForAnswer(Task<bool> answer, CancellationToken cancellationToken)
    {
        try
        {
            return await answer.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The answer is abandoned, so its fault, if it comes, is observed rather than left unobserved.
            _ = answer.ContinueWith(task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }

    /// <summary>
    /// Asks every registered <see cref="IJobAdmission"/>, in registration order, whether an execution may run.
    /// </summary>
    /// <param name="request">The request the job was started with, or null when it is not known.</param>
    /// <param name="checkPoint">Where in the job's life the question is asked, for the log.</param>
    /// <returns>
    /// <see cref="Admission.Refused"/> at the first refusal, <see cref="Admission.Failed"/> at the first exception, or when
    /// the bound for the answer has passed, and <see cref="Admission.Admitted"/> otherwise.
    /// </returns>
    /// <remarks>
    /// An implementation that throws or does not answer in time has not refused, so it never stops a job for good.
    /// </remarks>
    async Task<Admission> Admit(IJobRequest? request, string checkPoint)
    {
        var admissions = ResolveAdmissions();
        if (admissions.Count == 0)
        {
            return Admission.Admitted;
        }

        if (request is null)
        {
            _logger.AdmittingJobWithoutRequest(checkPoint);
            return Admission.Admitted;
        }

        var question = new JobAdmissionRequest(JobKey, JobId, State.Type, request);
        using var bound = new CancellationTokenSource(BoundedByResponseTimeout(Timeout.InfiniteTimeSpan));
        try
        {
            foreach (var admission in admissions)
            {
                // Awaited with the token as well as handed it: an implementation that ignores it cannot hold the turn.
                var answer = admission.MayRun(question, bound.Token);
                if (!await WaitForAnswer(answer, bound.Token))
                {
                    _logger.NotAdmitted(checkPoint);
                    return Admission.Refused;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.AdmissionFailed(ex, checkPoint);
            return Admission.Failed;
        }

        return Admission.Admitted;
    }

    List<IJobAdmission> ResolveAdmissions()
    {
        // A test host such as Orleans TestKit hands out an auto-mock for an unregistered enumerable, whose enumerator is null.
        var admissions = new List<IJobAdmission>();
        var registered = ServiceProvider.GetService<IEnumerable<IJobAdmission>>();
        if (registered?.GetEnumerator() is not { } enumerator)
        {
            return admissions;
        }

        using (enumerator)
        {
            while (enumerator.MoveNext())
            {
                admissions.Add(enumerator.Current);
            }
        }

        return admissions;
    }

    /// <summary>
    /// Refuses the resume of a job whose execution is not admitted.
    /// </summary>
    /// <returns>The refusal to report to the caller of <see cref="Resume"/>.</returns>
    /// <remarks>
    /// A job that was running is stopped and its drain made pending in one write, and the drain itself is left to a turn
    /// of its own: the owner awaits <see cref="Resume"/> in a turn of its own, so nothing is drained or fanned out here.
    /// </remarks>
    async Task<Result<ResumeJobSuccess, ResumeJobError>> RefuseResume()
    {
        if (State.Status is JobStatus.Running or JobStatus.StartingSteps)
        {
            await BeginDrain();
            _drainTimer?.Dispose();
            CompleteDrainAfterActivation();
        }

        return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.NotAdmitted);
    }

    /// <summary>
    /// Asks whether the next stage may start, and acts on a refusal or a failure to answer.
    /// </summary>
    /// <returns>True if the stage may start, false if the job was stopped or is waiting to ask again.</returns>
    async Task<bool> AdmitNextStage()
    {
        var admission = await Admit(State.Request, "Stage");
        if (admission is Admission.Admitted)
        {
            return true;
        }

        // A refusal that could not be written is not a stop, so it is asked about again like a failure to answer.
        if (admission is Admission.Refused && await StopForRefusedStage())
        {
            return false;
        }

        ArmStageAdmissionTimer();
        return false;
    }

    /// <summary>
    /// Stops the job because the next stage was refused.
    /// </summary>
    /// <returns>True if the stop is durable, false if it could not be written and was undone.</returns>
    async Task<bool> StopForRefusedStage()
    {
        var written = await WriteOrUndo(() => StatusChanged(JobStatus.Stopped));
        if (written.TryGetException(out _))
        {
            return false;
        }

        DisposeStageAdmissionTimer();
        return true;
    }

    void ArmStageAdmissionTimer()
    {
        if (_stageAdmissionTimer is not null)
        {
            return;
        }

        // Asked again every bound until it is answered, so a failure to answer is not a wait for the next restart.
        var bound = BoundedByResponseTimeout(Timeout.InfiniteTimeSpan);
        _stageAdmissionTimer = this.RegisterGrainTimer(
            _ => AskAgainAboutNextStage(),
            new GrainTimerCreationOptions { DueTime = bound, Period = bound });
    }

    void DisposeStageAdmissionTimer()
    {
        _stageAdmissionTimer?.Dispose();
        _stageAdmissionTimer = null;
    }

    async Task AskAgainAboutNextStage()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (!IsWaitingToAdmitNextStage)
            {
                // Whatever was waited for has happened - the stage started or the job moved on - and what that left
                // behind may have been held back by the wait.
                DisposeStageAdmissionTimer();
                if (State.Status is JobStatus.Running)
                {
                    _ = await HandleCompletionResult(await HandleCompletion());
                }

                return;
            }

            var admission = await Admit(State.Request, "Stage");
            if (admission is Admission.Admitted)
            {
                DisposeStageAdmissionTimer();
                _ = await ResumeStagesLeftBetween();
            }
            else if (admission is Admission.Refused && await StopForRefusedStage())
            {
                _ = await HandleCompletionResult(await HandleCompletion());
            }
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
        }
    }
}
