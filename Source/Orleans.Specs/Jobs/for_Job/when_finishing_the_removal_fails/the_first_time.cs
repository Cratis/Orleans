// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class the_first_time : given.a_job_whose_removal_could_not_be_finished
{
    void Because()
    {
    }

    [Fact] void should_schedule_one_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
    [Fact] void should_retry_after_one_second() => TimerOptions.Single().DueTime.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_use_a_one_shot_timer() => TimerOptions.Single().Period.ShouldEqual(Timeout.InfiniteTimeSpan);
    [Fact] void should_keep_the_activation_alive_between_retries() => TimerOptions.Single().KeepAlive.ShouldBeTrue();
    [Fact] void should_not_retry_before_the_timer_fires() => _job.ClearAttempts.ShouldEqual(1);
}
