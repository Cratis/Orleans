// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_step_awaiting_retry_is_still_stopped_in_storage : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    protected override int NumberOfSteps => 3;

    protected override JobsOptions CreateOptions() => new()
    {
        StepRetryBackoff = TimeSpan.FromHours(1),
        MaxStepRetryBackoff = TimeSpan.FromHours(1)
    };

    async Task Establish()
    {
        _job.RetryWhen = (id, _, _) => id == _stepIds[1];
        await Fail(1);
        StoredJobStep(_stepIds[1]).Status = JobStepStatus.Stopped;
        _steps[1].Invocations.Clear();
        AllowResume();
    }

    async Task Because() => await _job.Resume();

    [Fact] void should_recover_the_genuinely_stopped_step() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_not_start_the_step_awaiting_backoff() => StartsOf(1).ShouldEqual(0);
    [Fact] void should_not_retry_the_step_ahead_of_backoff() => _steps[1].Verify(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_not_count_a_terminal_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_not_record_a_terminal_outcome() => _job.CurrentState.CompletedStepsCounted.ShouldNotContain(_stepIds[1]);
}
