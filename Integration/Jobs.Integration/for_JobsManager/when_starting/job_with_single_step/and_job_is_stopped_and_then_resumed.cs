// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using context = Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_is_stopped_and_then_resumed.context;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(JobsClusterCollection.Name)]
public class and_job_is_stopped_and_then_resumed(context context) : Given<context>(context)
{
    public class context : a_jobs_manager
    {
        public Result<JobId, StartJobError> StartJobResult;
        public JobState CompletedJobState;
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
            await JobsManager.Resume(JobId);
            CompletedJobState = await JobStorage.WaitTillJobMeetsPredicate(JobId, state => state.Status == JobStatus.CompletedSuccessfully);
            JobSteps = await JobStepStorage.GetJobSteps(JobId);
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_not_keep_any_job_step_states_after_completed() => Context.JobSteps.ShouldBeEmpty();

    [Fact]
    public void should_have_completed_job_successfully() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    [Fact]
    public void should_have_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_stopped_job_once() => Context.CompletedJobState.StatusChanges.ShouldContain(_ => _.Status == JobStatus.Stopped);

    [Fact]
    public void should_have_job_progress_with_one_successful_step() => Context.CompletedJobState.Progress.SuccessfulSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_twice() => Context.JobStepProcessor.ShouldHavePerformedJobStepCalls(Context.JobId, 2);

    [Fact]
    public void should_have_completed_work_successfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.CompletedSuccessfully, 1);
}
