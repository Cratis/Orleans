// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections;
using Cratis.Serialization;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents a projection builder a state machine's projection is described on to learn what it consumes, without
/// registering it.
/// </summary>
/// <param name="eventTypes">The <see cref="EventTypesRecorder"/> recording the event types the projection refers to.</param>
/// <typeparam name="TModel">Type of read model.</typeparam>
internal sealed class InspectedProjectionBuilder<TModel>(EventTypesRecorder eventTypes)
    : ProjectionBuilderFor<TModel>(new ProjectionId(typeof(TModel).FullName!), typeof(TModel), new DefaultNamingPolicy(), eventTypes, new JsonSerializerOptions()),
      IProjectionBuilderFor<TModel>
{
    /// <summary>
    /// Gets the CLR event types the projection consumes.
    /// </summary>
    public IEnumerable<Type> EventTypes => eventTypes.Recorded;

    /// <summary>
    /// Gets a value indicating whether the projection was declared passive.
    /// </summary>
    public bool IsPassive { get; private set; }

    /// <inheritdoc/>
    IProjectionBuilderFor<TModel> IProjectionBuilderFor<TModel>.Passive()
    {
        IsPassive = true;
        return Passive();
    }
}
