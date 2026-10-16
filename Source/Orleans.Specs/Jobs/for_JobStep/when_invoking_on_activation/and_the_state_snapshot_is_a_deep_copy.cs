// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class and_the_state_snapshot_is_a_deep_copy : given.a_job_step_left_scheduled_by_a_dead_process
{
    void Establish() => _persistedState.Progress.Message = new("before");

    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_hand_out_the_live_state() => _jobStep.ReceivedState.ShouldNotBeSame(_jobStep.LiveState);
    [Fact] void should_not_hand_out_the_live_progress() => _jobStep.ReceivedState.Progress.ShouldNotBeSame(_jobStep.LiveState.Progress);
    [Fact] void should_not_hand_out_the_live_status_changes() => _jobStep.ReceivedState.StatusChanges.ShouldNotBeSame(_jobStep.LiveState.StatusChanges);
    [Fact] void should_copy_the_progress() => _jobStep.ReceivedState.Progress.Message.Value.ShouldEqual("before");
    [Fact] void should_copy_the_status_changes_up_to_running() => _jobStep.ReceivedState.StatusChanges[^1].Status.ShouldEqual(JobStepStatus.Running);

    void Destroy() => _jobStep.Dispose();
}
