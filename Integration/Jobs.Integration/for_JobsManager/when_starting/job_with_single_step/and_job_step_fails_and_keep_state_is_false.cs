// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using context = Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_step_fails_and_keep_state_is_false.context;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(JobsClusterCollection.Name)]
public class and_job_step_fails_and_keep_state_is_false(context context) : Given<context>(context)
{
    public class context : a_jobs_manager
    {
        public Result<JobId, StartJobError> StartJobResult;
        public JobState? Job;
        public IImmutableList<JobStepState> JobSteps = [];
        public JobId JobId;
        public Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError> GetJobResult;

        async Task Because()
        {
            JobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = false, ShouldFail = true });
            await JobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            Job = await JobStorage.WaitTillJobProgressCompleted(JobId);
            JobSteps = await JobStepStorage.GetJobSteps(JobId);
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_keep_job_state() => Context.Job.ShouldNotBeNull();

    [Fact]
    public void should_have_correct_job_type() => Context.Job.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_keep_state_of_failed_job_step() => Context.JobSteps.ShouldContainSingleItem();

    [Fact]
    public void should_have_job_step_state_where_status_is_failed() => Context.JobSteps[0].Status.ShouldEqual(JobStepStatus.CompletedWithFailure);

    [Fact]
    public void should_have_completed_job_progress() => Context.Job.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_failed_step() => Context.Job.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_completed_work_unsuccessfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.CompletedWithFailure, 1);
}
