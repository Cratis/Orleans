// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// A state machine that derives the state to activate into from its stored model rather than from the recorded current state.
/// </summary>
/// <param name="states">The states of the state machine.</param>
public class StateMachineResolvingActivationStateForTesting(IEnumerable<IState<StateMachineStateForTesting>> states) : StateMachine<StateMachineStateForTesting>
{
    public const string SupportedModel = "supported";

    readonly IImmutableList<IState<StateMachineStateForTesting>> _states = states.ToImmutableList();

    public override IImmutableList<IState<StateMachineStateForTesting>> CreateStates() => _states;

    protected override Type ResolveActivationState() =>
        State.Something == SupportedModel ? typeof(StateThatSupportsTransitioningFrom) : typeof(StateThatDoesNotSupportTransitioningFrom);
}
