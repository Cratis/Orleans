// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.StateMachines.for_StateMachine.given;

public abstract class a_state_machine_with_a_failing_drain : Specification
{
    protected TestKitSilo _silo;
    protected SuppliedStorageForTesting _storage;
    protected StateMachineWithSuppliedStorageForTesting _stateMachine;
    protected StateHoldingOnEnter _holding;
    protected StateThrowingOnLeave _throwingOnLeave;
    protected StateThrowingOnGuard _throwingOnGuard;
    protected Task _transition;
    protected Exception? _failure;
    protected int _staleEntries;

    async Task Establish()
    {
        _silo = new();
        _storage = new();
        _holding = new();
        _throwingOnLeave = new();
        _throwingOnGuard = new();
        _silo.AddService(_storage);
        _silo.AddService<IEnumerable<IState<StateMachineStateForTesting>>>(
        [
            _holding,
            new StateThrowingOnEnter(),
            _throwingOnLeave,
            _throwingOnGuard,
            new StateThatSupportsTransitioningFrom { OnEnterCalled = _ => _staleEntries++ },
            new StateThatDoesNotChangeStateOnSelfTransition()
        ]);
        _stateMachine = await _silo.CreateGrainAsync<StateMachineWithSuppliedStorageForTesting>(IdSpan.Create(string.Empty));
        _transition = _stateMachine.TransitionTo<StateHoldingOnEnter>();
        await _holding.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    protected Task Admit<TState>()
        where TState : IState<StateMachineStateForTesting> =>
        _stateMachine.TransitionTo<TState>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    protected async Task FinishDrain()
    {
        _holding.Release.SetResult();
        _failure = await Catch.Exception(() => _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    void Destroy() => _holding.Release.TrySetResult();
}
