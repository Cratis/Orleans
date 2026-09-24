// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using context = Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_is_stopped_and_then_deleted.context;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(JobsClusterCollection.Name)]
public class and_job_is_stopped_and_then_deleted(context context) : Given<context>(context)
{
    public class context : a_jobs_manager
    {
        public Result<JobId, StartJobError> StartJobResult;
        public IImmutableList<JobStepState> JobSteps = [];
        public JobId JobId;

        async Task Because()
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobStepProcessor.SetStartTask(taskCompletionSource.Task);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            JobId = StartJobResult.AsT0;
            await JobStepProcessor.WaitForAllPreparedStepsToBeStarted();
            await JobsManager.Stop(JobId);
            taskCompletionSource.SetResult();
            await JobStorage.WaitTillJobProgressStopped(JobId);
            await JobsManager.Delete(JobId);
            await JobStorage.WaitTillJobIsDeleted(JobId);
            JobSteps = await JobStepStorage.GetJobSteps(JobId);
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_remove_state_of_job_step() => Context.JobSteps.ShouldBeEmpty();

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_stopped_work_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.Stopped, 1);
}
