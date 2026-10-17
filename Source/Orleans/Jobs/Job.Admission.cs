// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        // Not GetServices: it throws where the service is not registered at all.
        var admissions = (ServiceProvider.GetService<IEnumerable<IJobAdmission>>() ?? []).ToArray();
        if (admissions.Length == 0)
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
                if (!await admission.MayRun(question, bound.Token).WaitAsync(bound.Token))
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

        _stageAdmissionTimer?.Dispose();
        _stageAdmissionTimer = null;
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

    async Task AskAgainAboutNextStage()
    {
        if (State.Status is not JobStatus.Running || !IsStaged || State.Progress.Stages.Current() is not { IsStarted: false })
        {
            _stageAdmissionTimer?.Dispose();
            _stageAdmissionTimer = null;
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            var admission = await Admit(State.Request, "Stage");
            if (admission is Admission.Admitted)
            {
                _stageAdmissionTimer?.Dispose();
                _stageAdmissionTimer = null;
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
