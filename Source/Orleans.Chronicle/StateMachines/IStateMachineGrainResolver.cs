// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines how a delivered event finds the state machine grain it refreshes.
/// </summary>
public interface IStateMachineGrainResolver
{
    /// <summary>
    /// Get the grain of a state machine type for a key.
    /// </summary>
    /// <param name="stateMachineType">The type of state machine - the grain class.</param>
    /// <param name="key">The <see cref="StateMachineKey"/>.</param>
    /// <returns>The <see cref="IEventSourcedStateMachine"/> grain.</returns>
    IEventSourcedStateMachine Get(Type stateMachineType, StateMachineKey key);
}
