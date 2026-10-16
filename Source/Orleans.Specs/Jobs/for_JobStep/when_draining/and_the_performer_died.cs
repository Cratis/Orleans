// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_the_performer_died : given.a_job_step_left_scheduled_by_a_dead_process
{
    JobStepDrainResult _result;

    void Establish() => _jobStep.CancelledResult = "partial";

    async Task Because() => _result = await _jobStep.Drain(1, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_answer_stopped() => _result.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_answer_the_result_from_the_checkpoint() => _result.Outcome!.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_persist_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_perform_any_work() => _jobStep.WorkRuns.ShouldEqual(0);

    async Task Destroy()
    {
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
