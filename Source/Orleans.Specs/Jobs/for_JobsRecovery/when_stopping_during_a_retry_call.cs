// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_stopping_during_a_retry_call : given.a_recovery_participant
{
    readonly TaskCompletionSource _retryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _neverCompletes = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _first.Rehydrate().Returns(
        _ => Task.FromException(new Exception("manager unavailable")),
        _ =>
        {
            _retryEntered.TrySetResult();
            return _neverCompletes.Task;
        });

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        delay.Elapse();
        await _retryEntered.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_stop_without_waiting_for_the_manager_call_to_return() => _neverCompletes.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_start_another_manager_call() => _first.Received(2).Rehydrate();
}
