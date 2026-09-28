// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when a state machine transition is declared for a type that is not a Chronicle event
/// type, nor an interface or abstract type event types can implement.
/// </summary>
/// <param name="type">The type the transition was declared for.</param>
public class TransitionEventIsNotAnEventType(Type type)
    : Exception($"A state machine transition can not be declared for '{type.FullName}' - it is not an event type. Mark it with [EventType], or use an interface or abstract type that event types implement.");
