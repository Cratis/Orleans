// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_cannot_be_resolved;

public class and_stopping_during_its_discovery : given.a_recovery_participant_with_unresolved_provider
{
    readonly TaskCompletionSource _discoveryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IEnumerable<JobsManagerKey>> _discovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationToken _discoveryToken;

    void Establish() => _unresolved.GetKeys(Arg.Any<CancellationToken>()).Returns(call =>
    {
        _discoveryToken = call.Arg<CancellationToken>();
        _discoveryToken.Register(() => _discovery.TrySetCanceled(_discoveryToken));
        _discoveryEntered.TrySetResult();
        return _discovery.Task;
    });

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        delay.Elapse();
        await _discoveryEntered.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_forward_the_shutdown_token_to_the_resolved_provider() => _discoveryToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_cancel_the_pending_discovery() => _discovery.Task.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_start_another_resolution() => _resolutions.ShouldEqual(3);
    [Fact] void should_not_rehydrate_the_unfinished_catalogs_manager() => _first.DidNotReceive().Rehydrate();
}
