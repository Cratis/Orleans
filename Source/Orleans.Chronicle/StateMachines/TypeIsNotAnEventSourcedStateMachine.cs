// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when a type registered as an event-sourced state machine is not a concrete
/// <see cref="EventSourcedStateMachine{TModel}"/>.
/// </summary>
/// <param name="type">The type.</param>
public class TypeIsNotAnEventSourcedStateMachine(Type type)
    : Exception($"'{type.FullName}' is not a concrete event-sourced state machine - it must be a non-abstract, non-generic class deriving from EventSourcedStateMachine<TModel>");
