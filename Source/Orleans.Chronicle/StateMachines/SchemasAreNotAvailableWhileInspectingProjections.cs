// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when describing a state machine's projection asks for an event type schema, which is
/// not available while the projection is only being inspected for the events it consumes.
/// </summary>
/// <param name="eventTypeId">The <see cref="EventTypeId"/> a schema was asked for.</param>
public class SchemasAreNotAvailableWhileInspectingProjections(EventTypeId eventTypeId)
    : Exception($"The schema for event type '{eventTypeId}' was asked for while inspecting a state machine projection for the events it consumes - schemas are not available then");
