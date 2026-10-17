// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class with_a_backoff_that_doubles : given.the_job
{
    protected override JobsOptions CreateOptions() => new() { StepRetryBackoff = TimeSpan.FromSeconds(5), MaxStepRetryBackoff = TimeSpan.FromSeconds(30) };

    [Fact] void should_wait_the_backoff_after_the_first_failure() => _job.DelayAfter(1).ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_double_it_after_the_second() => _job.DelayAfter(2).ShouldEqual(TimeSpan.FromSeconds(10));
    [Fact] void should_double_it_again_after_the_third() => _job.DelayAfter(3).ShouldEqual(TimeSpan.FromSeconds(20));
    [Fact] void should_never_wait_longer_than_the_maximum() => _job.DelayAfter(4).ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_not_overflow_for_a_large_attempt() => _job.DelayAfter(500).ShouldEqual(TimeSpan.FromSeconds(30));
}
