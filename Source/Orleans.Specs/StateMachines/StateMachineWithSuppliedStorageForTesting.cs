// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// A state machine that is given its storage rather than having it configured through a storage provider.
/// </summary>
/// <param name="states">The states of the state machine.</param>
/// <param name="suppliedStorage">The <see cref="SuppliedStorageForTesting"/> holding the storage to use.</param>
public class StateMachineWithSuppliedStorageForTesting(IEnumerable<IState<StateMachineStateForTesting>> states, SuppliedStorageForTesting suppliedStorage)
    : StateMachine<StateMachineStateForTesting>(suppliedStorage.Storage)
{
    readonly IImmutableList<IState<StateMachineStateForTesting>> _states = states.ToImmutableList();

    public override IImmutableList<IState<StateMachineStateForTesting>> CreateStates() => _states;

    public StateMachineStateForTesting StoredState => State;
}
