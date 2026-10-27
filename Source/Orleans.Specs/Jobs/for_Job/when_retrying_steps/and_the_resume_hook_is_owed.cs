// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_resume_hook_is_owed : given.a_running_job_with_steps
{
    TimeSpan _holdDelay;
    TimeSpan _resumedDelay;
    int _heldTimers;
    int _resumedTimers;

    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero };

    GrainTimerCreationOptions RetryTimerOptions => _silo.TimerRegistry.Mock.Invocations
        .SelectMany(invocation => invocation.Arguments).OfType<GrainTimerCreationOptions>()
        .Last(options => options.Period == Timeout.InfiniteTimeSpan);

    async Task Establish()
    {
        await Fail(0);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
        CountStepsInStorage((JobStepStatus.Running, 2));
    }

    async Task Because()
    {
        await _job.RetryDueSteps();
        await _silo.TimerRegistry.FireAllAsync();
        _holdDelay = RetryTimerOptions.DueTime;
        _heldTimers = _silo.TimerRegistry.NumberOfActiveTimers;
        _job.BeforeResuming = () => Task.CompletedTask;
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
        _resumedDelay = RetryTimerOptions.DueTime;
        _resumedTimers = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    [Fact] void should_not_retry_before_the_hook_succeeds() => _steps[0].Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_not_count_a_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_wait_before_checking_the_hook_again() => _holdDelay.ShouldBeGreaterThanOrEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_keep_one_retry_timer_beside_the_watchdog() => _heldTimers.ShouldEqual(2);
    [Fact] void should_rearm_immediately_after_the_hook_succeeds() => _resumedDelay.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_replace_the_held_retry_timer() => _resumedTimers.ShouldEqual(2);
}
