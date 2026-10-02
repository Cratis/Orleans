// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_deactivating_while_the_drain_is_finishing : given.a_manager_with_a_pending_drain
{
    bool _waitedForDrain;

    async Task Because()
    {
        var deactivation = _manager.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _waitedForDrain = !deactivation.IsCompleted;
        _pendingDrain.SetResult();
        await deactivation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivated = true;
    }

    [Fact] void should_wait_for_the_canceled_drain_to_finish() => _waitedForDrain.ShouldBeTrue();
}
