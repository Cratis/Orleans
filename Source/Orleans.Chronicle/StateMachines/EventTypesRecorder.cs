// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an <see cref="IEventTypes"/> that records every event type a projection definition refers to, so the
/// event types a projection consumes can be learned by describing it on a projection builder.
/// </summary>
/// <param name="knownEventTypes">The CLR event types the client knows, which is what an interface or base type in a projection expands to.</param>
/// <remarks>
/// Every event type a projection builder puts in a definition - from, join, removed with, children and nested - passes
/// through <see cref="GetEventTypeFor"/>. Answering it from the <see cref="EventTypeAttribute"/> on the type is enough to
/// build the definition, which is only inspected and never registered.
/// </remarks>
internal sealed class EventTypesRecorder(IEnumerable<Type> knownEventTypes) : IEventTypes
{
    readonly HashSet<Type> _recorded = [];

    /// <summary>
    /// Gets the CLR event types the projection refers to.
    /// </summary>
    public IEnumerable<Type> Recorded => _recorded;

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes { get; } = [.. knownEventTypes];

    /// <inheritdoc/>
    public IImmutableList<EventType> All => [.. AllClrTypes.Select(_ => _.GetEventType())];

    /// <inheritdoc/>
    public Task Discover() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Register() => Task.CompletedTask;

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => AllClrTypes.Any(_ => _.GetEventType().Id == eventTypeId);

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => AllClrTypes.Contains(clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => AllClrTypes.First(_ => _.GetEventType().Id == eventTypeId);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) =>
        AllClrTypes.First(_ => _.GetEventType() == new EventType(eventTypeId, generation));

    /// <inheritdoc/>
    public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => throw new SchemasAreNotAvailableWhileInspectingProjections(eventTypeId);

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType)
    {
        _recorded.Add(clrType);
        return clrType.GetEventType();
    }
}
