// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeInterruptedSteps()
    {
        if (!JobIsPrepared())
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobIsNotPrepared);
        }

        // Reload even in an existing activation: persisted Running is not proof of a live performer.
        // In particular, a durable stopped step must not be restarted by automatic recovery.
        _jobStepGrains = await GetIdAndGrainReferenceJobSteps(JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Unknown);
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
        return await ResumeCurrentStage(reportAlreadyRunning: true);
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> ResumeCurrentStage(bool reportAlreadyRunning)
    {
        var failedSteps = new List<JobStepId>();
        var allLive = true;
        var steps = StepsInCurrentStage();
        foreach (var chunk in steps.Chunk(MaxConcurrentJobStepStartups))
        {
            var results = await Task.WhenAll(chunk.Select(async step =>
                (step.Key, Grain: step.Value, Result: await step.Value.Start(this.GetGrainId()))));
            foreach (var (id, grain, result) in results)
            {
                allLive &= result.TryGetError(out var error) && error is StartJobStepError.AlreadyStarted;
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
