// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_the_job_was_never_resumed : given.a_running_job_with_two_steps
{
    void Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
    }

    async Task Because() => await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_count_the_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_record_its_identity() => _job.CurrentState.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_persist_an_empty_record() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldBeEmpty();
}
