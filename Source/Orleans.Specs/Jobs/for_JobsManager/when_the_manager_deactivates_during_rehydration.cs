// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_the_manager_deactivates_during_rehydration : given.a_controlled_backlog
{
    bool _stoppedBeforeInFlightResumesFinished;

    async Task Because()
    {
        await _manager.Rehydrate();
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        await _manager.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _deactivated = true;
        await WaitForRehydration();
        _stoppedBeforeInFlightResumesFinished = !_completions[0].Task.IsCompleted;
        CompleteResumes();
    }

    [Fact] void should_stop_without_waiting_for_in_flight_resumes() => _stoppedBeforeInFlightResumesFinished.ShouldBeTrue();
    [Fact] void should_not_dispatch_the_remaining_jobs() => _resumed.Count.ShouldEqual(2);
}
