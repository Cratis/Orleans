// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class and_the_retry_fails_again : given.a_job_whose_removal_could_not_be_finished
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_try_to_clear_the_job_again() => _job.ClearAttempts.ShouldEqual(2);
    [Fact] void should_keep_one_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
    [Fact] void should_double_the_delay() => TimerOptions.Select(options => options.DueTime.TotalSeconds).ShouldEqual([1, 2]);
    [Fact] void should_keep_the_activation_alive_until_the_next_retry() => TimerOptions.Last().KeepAlive.ShouldBeTrue();
    [Fact] void should_still_be_removing() => _job.CurrentState.Status.ShouldEqual(JobStatus.Removing);
}
