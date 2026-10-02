// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_orleans_cancels_teardown_while_the_drain_is_finishing : given.a_manager_with_a_pending_drain
{
    bool _drainWasStillPending;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var deactivation = _manager.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), cancellation.Token);
        await cancellation.CancelAsync();
        await deactivation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivated = true;
        _drainWasStillPending = !_pendingDrain.Task.IsCompleted;
    }

    [Fact] void should_stop_waiting_when_orleans_cancels_teardown() => _drainWasStillPending.ShouldBeTrue();
}
