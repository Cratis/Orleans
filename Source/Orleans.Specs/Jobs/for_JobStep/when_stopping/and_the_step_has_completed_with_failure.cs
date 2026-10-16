// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_has_completed_with_failure : given.a_job_step_left_scheduled_by_a_dead_process
{
    void Establish() => _persistedState.Status = JobStepStatus.CompletedWithFailure;

    async Task Because() => await _jobStep.OnJobStopped();

    [Fact] void should_preserve_the_terminal_failure() => _persistedState.Status.ShouldEqual(JobStepStatus.CompletedWithFailure);
    [Fact] void should_not_write_another_status() => _persistedState.StatusChanges.ShouldBeEmpty();

    void Destroy() => _jobStep.Dispose();
}
