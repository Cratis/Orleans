// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateExtensions;

public class when_setting_state_machine : Specification
{
    StateThatSupportsTransitioningFrom _state;
    IStateMachine<StateMachineStateForTesting> _stateMachine;

    void Establish()
    {
        _state = new();
        _stateMachine = Substitute.For<IStateMachine<StateMachineStateForTesting>>();
    }

    void Because() => _state.SetStateMachine(_stateMachine);

    [Fact] void should_set_the_state_machine_on_the_state() => _state.StateMachine.ShouldEqual(_stateMachine);
}
