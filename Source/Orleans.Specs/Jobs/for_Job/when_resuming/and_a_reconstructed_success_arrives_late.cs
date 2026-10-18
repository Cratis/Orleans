// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_reconstructed_success_arrives_late : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.StepsToPrepare.RemoveAll(step => step.Id == _laterId);
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        await ActivateFrom(JobStatus.StartingSteps);
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();

    [Fact] void should_count_the_success_only_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_keep_the_unfinished_step_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_handle_an_already_accounted_outcome() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_drop_the_plan_after_recovery_reaches_running() => _job.WrittenStates.Last(state => state.Status is JobStatus.Running).StepPlan.ShouldBeNull();
}
