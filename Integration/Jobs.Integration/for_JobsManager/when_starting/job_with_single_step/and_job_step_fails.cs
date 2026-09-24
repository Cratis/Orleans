// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using context = Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_step_fails.context;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(JobsClusterCollection.Name)]
public class and_job_step_fails(context context) : Given<context>(context)
{
    public class context : a_jobs_manager
    {
        public Result<JobId, StartJobError> StartJobResult;
        public JobState CompletedJobState;
        public IImmutableList<JobStepState> JobStep = [];
        public JobId JobId;

        async Task Because()
        {
            JobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true, ShouldFail = true });
            await JobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            var getJobState = await JobStorage.GetJob(JobId);
            var getJobStepState = await JobStepStorage.GetForJob(JobId);

            CompletedJobState = await JobStorage.WaitTillJobProgressCompleted(JobId);
            CompletedJobState = await JobStorage.WaitTillJobMeetsPredicate(JobId, state => state.Status is JobStatus.CompletedWithFailures);

            // The job step grain persists its state asynchronously after the job aggregates progress.
            // Poll until the step's persisted status reflects the failure.
            using var stepCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            do
            {
                await Task.Delay(100, stepCts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                JobStep = await JobStepStorage.GetJobSteps(JobId);
            } while (JobStep.All(s => s.Status != JobStepStatus.CompletedWithFailure) && !stepCts.IsCancellationRequested);
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_keep_job_state() => Context.CompletedJobState.ShouldNotBeNull();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_keep_state_of_failed_job_step() => Context.JobStep.ShouldContainSingleItem();

    [Fact]
    public void should_have_job_step_state_where_status_is_failed() => Context.JobStep[0].Status.ShouldEqual(JobStepStatus.CompletedWithFailure);

    [Fact]
    public void should_have_completed_job_with_failure() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.CompletedWithFailures);

    [Fact]
    public void should_have_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_failed_step() => Context.CompletedJobState.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_completed_work_unsuccessfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.CompletedWithFailure, 1);
}
