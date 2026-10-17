// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_the_job_is_stopped : given.a_window_of_two_over_five_steps
{
    async Task Because()
    {
        await _job.Stop();
        await _job.OnStepStopped(_stepIds[0], JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
        await _job.OnStepStopped(_stepIds[1], JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
    }

    [Fact] void should_account_for_the_steps_that_never_started_as_stopped() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(5);
    [Fact] void should_have_every_step_accounted_for() => _job.CurrentState.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_not_start_more_steps() => Started.ShouldEqual(2);
}
