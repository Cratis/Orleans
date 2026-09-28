// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines the definitions of the event-sourced state machines in the application, as inspected at startup.
/// </summary>
public interface IEventSourcedStateMachineDefinitions
{
    /// <summary>
    /// Gets every <see cref="EventSourcedStateMachineDefinition"/>.
    /// </summary>
    IEnumerable<EventSourcedStateMachineDefinition> All { get; }
}
