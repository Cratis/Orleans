// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    static bool IsAlreadyStarted(Result<StartJobStepError> result) =>
        result.TryGetError(out var error) && error is StartJobStepError.AlreadyStarted;

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
            return await ResumeCurrentStage(tracked, reportAlreadyRunning: true);
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
        return await ResumeCurrentStage(StepsInCurrentStage(), reportAlreadyRunning: true);
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeCurrentStage(Dictionary<JobStepId, IJobStep> steps, bool reportAlreadyRunning)
    {
        _hasLiveStepTracking = true;
        var failedSteps = new List<JobStepId>();
        var allLive = true;
        foreach (var chunk in steps.Chunk(MaxConcurrentJobStepStartups))
        {
            (JobStepId Key, IJobStep Grain, Result<StartJobStepError> Result)[] results;
            using (JobExecutionGeneration.Carry(ExecutionGeneration()))
            {
                results = await Task.WhenAll(chunk.Select(async step =>
                    (step.Key, Grain: step.Value, Result: await step.Value.Start(this.GetGrainId()))));
            }
            foreach (var (id, grain, result) in results)
            {
                allLive &= IsAlreadyStarted(result);
                if (await HandleResumeJobStepResult(id, result, grain))
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
