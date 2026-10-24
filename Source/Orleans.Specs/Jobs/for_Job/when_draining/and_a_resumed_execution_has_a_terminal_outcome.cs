// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_a_resumed_execution_has_a_terminal_outcome : given.a_running_job_with_two_steps
{
    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
        DrainsAs(_first, _firstId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
    }

    async Task Because() => await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_record_only_the_terminal_outcome() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_firstId);
    [Fact] void should_persist_the_terminal_record() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_firstId);
    [Fact] void should_keep_stops_in_their_own_record() => _job.CurrentState.StoppedStepsCounted.ShouldContainOnly(_secondId);
}
