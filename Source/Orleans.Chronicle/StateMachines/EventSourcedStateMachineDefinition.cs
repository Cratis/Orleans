// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Reactors;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents what the integration learned about an event-sourced state machine at startup: its model, where the
/// model's projection comes from, and the events it is interested in.
/// </summary>
/// <param name="StateMachineType">The type of state machine.</param>
/// <param name="ModelType">The type of read model the state machine follows.</param>
/// <param name="ProjectionSource">Where the projection maintaining the model comes from.</param>
/// <param name="IsPassive">Whether the model is passive - computed from the event log on demand rather than materialized.</param>
/// <param name="ProjectionEventTypes">The event types the model's projection consumes.</param>
/// <param name="TransitionEventTypes">The event types the declared transitions are taken for, with interfaces and base types expanded to the event types implementing them.</param>
public record EventSourcedStateMachineDefinition(
    Type StateMachineType,
    Type ModelType,
    StateMachineProjectionSource ProjectionSource,
    bool IsPassive,
    IImmutableSet<Type> ProjectionEventTypes,
    IImmutableSet<Type> TransitionEventTypes)
{
    /// <summary>
    /// Gets the event types the state machine is interested in - the union of what its projection consumes and what its
    /// transitions are taken for. This is exactly what its reactor subscribes to.
    /// </summary>
    public IImmutableSet<Type> EventTypes => ProjectionEventTypes.Union(TransitionEventTypes);

    /// <summary>
    /// Gets the <see cref="Cratis.Chronicle.Reactors.ReactorId"/> of the reactor delivering the events to the state machine.
    /// </summary>
    public ReactorId ReactorId => new(StateMachineType.FullName!);

    /// <summary>
    /// Gets the registration of the projection, when the integration registers it.
    /// </summary>
    internal Action<IStateMachineArtifactRegistrations>? RegisterProjection { get; init; }
}
