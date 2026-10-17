// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_fails;

public class with_a_failure_the_job_does_not_retry : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3 };

    void Establish() => _job.RetryWhen = (_, result, _) => result.TryGetError(out var error) && error.ErrorMessages.Contains("timed out");

    Task Because() => Fail(0);

    [Fact] void should_count_the_failure_whatever_attempts_are_left() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
