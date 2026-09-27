// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_activating;

public class with_activation_state_resolved_by_the_state_machine : Specification
{
    TestKitSilo _silo;
    StateMachineResolvingActivationStateForTesting _stateMachine;
    StateThatSupportsTransitioningFrom _resolvedState;
    StateMachineStateForTesting _onEnterCalledState;
    IStorage<StateMachineStateForTesting> _stateStorage;

    void Establish()
    {
        _silo = new();
        _resolvedState = new() { OnEnterCalled = _ => _onEnterCalledState = _ };
        _silo.AddService<IEnumerable<IState<StateMachineStateForTesting>>>([new StateThatDoesNotSupportTransitioningFrom(), _resolvedState]);

        _stateStorage = _silo.StorageManager.GetStorage<StateMachineStateForTesting>(typeof(StateMachineResolvingActivationStateForTesting).FullName);
        _stateStorage.State = new()
        {
            CurrentState = typeof(StateThatDoesNotSupportTransitioningFrom).FullName,
            Something = StateMachineResolvingActivationStateForTesting.SupportedModel
        };
    }

    async Task Because() => _stateMachine = await _silo.CreateGrainAsync<StateMachineResolvingActivationStateForTesting>(IdSpan.Create(string.Empty));

    [Fact] async Task should_enter_the_state_resolved_by_the_state_machine() => (await _stateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioningFrom>();
    [Fact] void should_call_on_enter_with_the_stored_model() => _onEnterCalledState.Something.ShouldEqual(StateMachineResolvingActivationStateForTesting.SupportedModel);
    [Fact] void should_record_the_resolved_state_as_current_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(StateThatSupportsTransitioningFrom).FullName);
}
