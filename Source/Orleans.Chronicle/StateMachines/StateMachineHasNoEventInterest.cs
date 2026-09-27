// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when an event-sourced state machine is interested in no event types at all - its
/// projection consumes none and it declares no transitions.
/// </summary>
/// <param name="stateMachineType">The type of state machine.</param>
public class StateMachineHasNoEventInterest(Type stateMachineType)
    : Exception($"The state machine '{stateMachineType.FullName}' is interested in no event types - neither its projection nor its transitions refer to any known event type");
