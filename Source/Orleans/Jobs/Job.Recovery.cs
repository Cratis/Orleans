// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _resumeStartFailures = [];

    static bool IsAlreadyStarted(Result<StartJobStepError> result) =>
        result.TryGetError(out var error) && error is StartJobStepError.AlreadyStarted;

    async Task<bool> TryRunResumeHook(JobProgress? stoppedProgress)
    {
        try
        {
            // Running must already be durable: consumers may adopt this job from their own replay entry without
            // recursively resuming the job whose hook is waiting on them.
            await OnBeforeResumingJobSteps();
            await RunDispatchHook(initializationMayBeIncomplete: false);
            return true;
        }
        catch (Exception exception)
        {
            _logger.Failed(exception);
            if (stoppedProgress is not null)
            {
                State.Progress = stoppedProgress;
                if (!(await WriteStatusChanged(JobStatus.Stopped, exception)).IsSuccess)
                {
                    // Compensation may or may not be durable. Do not let restored counters serve a persisted
                    // Running execution; the next activation must recover whichever write reached storage.
                    DeactivateOnIdleDuringShutdown();
                }
            }
            return false;
        }
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeInterruptedSteps()
    {
        if (!JobIsPrepared())
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobIsNotPrepared);
        }

        var restartStoppedSteps = !_hasLiveStepTracking && State.Status is JobStatus.Running && StoppedStepsPredateLastRunning();
        var resumable = restartStoppedSteps
            ? await GetIdAndGrainReferenceForNonCompletedJobSteps()
            : await GetIdAndGrainReferenceJobSteps(JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Unknown);
        if (_hasLiveStepTracking)
        {
            // Storage and outcome delivery are not atomic: success deletes storage before its report,
            // failure reports before moving storage. Only this activation knows which reports it accepted.
            var tracked = StepsInCurrentStage().Where(step => resumable.ContainsKey(step.Key))
                .ToDictionary(step => step.Key, step => step.Value);
            await OnBeforeResumingJobSteps();
            await RunDispatchHook(initializationMayBeIncomplete: false);
            return await ResumeCurrentStage(tracked, reportAlreadyRunning: _hasDispatchedSteps);
        }

        // Reconstruct a cold activation from durable state. A surviving step can still have an outcome queued
        // behind this resume; outcome handlers must ignore it if reconstruction finalizes or clears the job.
        if (restartStoppedSteps)
        {
            _logger.RestartingStepsStoppedBeforeLastResume();
        }
        _jobStepGrains = resumable;
        if (_jobStepGrains.Count == 0)
        {
            var stopped = await GetIdAndGrainReferenceJobSteps(JobStepStatus.Stopped);
            if (stopped.Count > 0)
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
            }
            await ReconcileProgressFromJobSteps();
            return await FinalizeInsteadOfResuming();
        }
        if (await PrepareStagesForResume())
        {
            return await FinalizeInsteadOfResuming();
        }

        await OnBeforeResumingJobSteps();
        await RunDispatchHook(initializationMayBeIncomplete: false);
        return await ResumeCurrentStage(StepsInCurrentStage(), reportAlreadyRunning: _hasDispatchedSteps);
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeCurrentStage(Dictionary<JobStepId, IJobStep> steps, bool reportAlreadyRunning)
    {
        await EnsureDispatchHook();
        _hasDispatchedSteps = true;
        _hasLiveStepTracking = true;
        steps = SelectWindow(steps);
        var failedSteps = new List<JobStepId>();
        var allLive = true;
        foreach (var chunk in steps.Chunk(MaxConcurrentJobStepStartups))
        {
            (JobStepId Id, Result<StartJobStepError> Result, Exception? Exception)[] results;
            using (JobExecutionGeneration.Carry(ExecutionGeneration()))
            {
                results = await Task.WhenAll(chunk.Select(step => TryStartJobStep(step.Key, step.Value, this.GetGrainId())));
            }
            foreach (var (id, result, exception) in results)
            {
                allLive &= IsAlreadyStarted(result);
                if (exception is not null)
                {
                    _logger.FailedStartingJobStep(exception, id);
                    _resumeStartFailures.Add(id);
                    failedSteps.Add(id);

                    // Dispatch may have reached the step despite the exception; it must still hear a stop or removal.
                    await TrySubscribeJobStep(id, steps[id]);
                    continue;
                }

                // An explicit answer is no longer an uncertain dispatch, including a refusal that must not be retried.
                _resumeStartFailures.Remove(id);
                if (await HandleResumeJobStepResult(id, result, steps[id]))
                {
                    failedSteps.Add(id);
                }
            }
        }

        if (failedSteps.Count > 0)
        {
            return Result<ResumeJobSuccess, ResumeJobError>.Failed(new FailedResumingJobSteps(failedSteps));
        }
        return reportAlreadyRunning && steps.Count > 0 && allLive
            ? ResumeJobSuccess.JobAlreadyRunning
            : ResumeJobSuccess.Success;
    }
}
