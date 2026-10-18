// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_the_terminal_write_fails : given.a_job_ready_to_complete
{
    void Establish() => _failWrites = true;

    async Task Because()
    {
        await _job.Resume();
        await HandleCompletion();
        await _job.OnStepSucceeded(JobStepId.New(), JobStepResult.Succeeded());
    }

    [Fact] void should_keep_the_activation_alive() => Deactivations.ShouldEqual(0);
    [Fact] void should_not_clear_a_job_whose_terminal_status_is_not_durable() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_schedule_one_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
    [Fact] void should_retry_after_one_second() => TimerOptions.Single().DueTime.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_use_a_one_shot_timer() => TimerOptions.Single().Period.ShouldEqual(Timeout.InfiniteTimeSpan);
    [Fact] void should_keep_the_activation_alive_between_retries() => TimerOptions.Single().KeepAlive.ShouldBeTrue();
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_retry_the_write_before_the_timer_fires() => _writeAttempts.ShouldEqual(1);
}
