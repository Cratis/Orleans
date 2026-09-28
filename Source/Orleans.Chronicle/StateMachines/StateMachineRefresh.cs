// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Orleans.Serialization;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents the delivery of an event an event-sourced state machine is interested in, as it crosses the grain boundary.
/// </summary>
/// <param name="EventTypeName">The assembly qualified name of the CLR type of the delivered event.</param>
/// <param name="SerializedContext">The <see cref="EventContext"/> of the delivered event, in its JSON form.</param>
/// <remarks>
/// Both travel as strings so that the grain call does not depend on how the host has configured Orleans serialization:
/// application event types are rarely known to the Orleans type manifest, and a <see cref="Type"/> naming one would be
/// rejected as soon as the call crosses a silo boundary.
/// </remarks>
[GenerateSerializer]
[Immutable]
public sealed record StateMachineRefresh([property: Id(0)] string EventTypeName, [property: Id(1)] string SerializedContext)
{
    static readonly JsonSerializerOptions _serializerOptions = CratisJsonSerializer.CreateSerializerOptions();

    /// <summary>
    /// Gets the CLR type of the delivered event.
    /// </summary>
    public Type EventType => Type.GetType(EventTypeName, throwOnError: true)!;

    /// <summary>
    /// Create a <see cref="StateMachineRefresh"/> for a delivered event.
    /// </summary>
    /// <param name="eventType">The CLR type of the delivered event.</param>
    /// <param name="context">The <see cref="EventContext"/> of the delivered event.</param>
    /// <returns>The <see cref="StateMachineRefresh"/>.</returns>
    public static StateMachineRefresh For(Type eventType, EventContext context) =>
        new(eventType.AssemblyQualifiedName!, JsonSerializer.Serialize(context, _serializerOptions));

    /// <summary>
    /// Get the <see cref="EventContext"/> of the delivered event.
    /// </summary>
    /// <returns>The <see cref="EventContext"/>.</returns>
    public EventContext GetContext() => JsonSerializer.Deserialize<EventContext>(SerializedContext, _serializerOptions)!;
}
