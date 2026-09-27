// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// Represents the base state of a state machine.
/// </summary>
/// <remarks>
/// When the stored state of a <see cref="StateMachine{TStoredState}"/> derives from this type, the state machine records
/// the full name of the current state type in <see cref="CurrentState"/> after every transition, and restores the current
/// state from it on activation.
/// </remarks>
public class StateMachineState
{
    /// <summary>
    /// Gets or sets the current state.
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;
}
