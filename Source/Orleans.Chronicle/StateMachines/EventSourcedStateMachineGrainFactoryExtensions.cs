// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Extension methods for getting event-sourced state machine grains from an <see cref="IGrainFactory"/>.
/// </summary>
public static class EventSourcedStateMachineGrainFactoryExtensions
{
    /// <summary>
    /// Get the state machine grain for a <see cref="StateMachineKey"/>.
    /// </summary>
    /// <typeparam name="TStateMachine">The grain interface of the state machine.</typeparam>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="key">The <see cref="StateMachineKey"/>.</param>
    /// <returns>The grain.</returns>
    public static TStateMachine GetEventSourcedStateMachine<TStateMachine>(this IGrainFactory grainFactory, StateMachineKey key)
        where TStateMachine : IEventSourcedStateMachine =>
        grainFactory.GetGrain<TStateMachine>(key.ToString());

    /// <summary>
    /// Get the state machine grain following an event source in the event store and namespace of an <see cref="IEventStore"/>.
    /// </summary>
    /// <typeparam name="TStateMachine">The grain interface of the state machine.</typeparam>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> the events live in - typically the tenant-resolved one of the current request.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to follow.</param>
    /// <returns>The grain.</returns>
    public static TStateMachine GetEventSourcedStateMachine<TStateMachine>(this IGrainFactory grainFactory, IEventStore eventStore, EventSourceId eventSourceId)
        where TStateMachine : IEventSourcedStateMachine =>
        grainFactory.GetEventSourcedStateMachine<TStateMachine>(StateMachineKey.From(eventStore, eventSourceId));

    /// <summary>
    /// Get the state machine grain following an event source in an event store, in a namespace or the default one.
    /// </summary>
    /// <typeparam name="TStateMachine">The grain interface of the state machine.</typeparam>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the events live in.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to follow.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the events live in. Defaults to <see cref="EventStoreNamespaceName.Default"/>.</param>
    /// <returns>The grain.</returns>
    public static TStateMachine GetEventSourcedStateMachine<TStateMachine>(
        this IGrainFactory grainFactory,
        EventStoreName eventStore,
        EventSourceId eventSourceId,
        EventStoreNamespaceName? @namespace = null)
        where TStateMachine : IEventSourcedStateMachine =>
        grainFactory.GetEventSourcedStateMachine<TStateMachine>(new StateMachineKey(eventStore, @namespace ?? EventStoreNamespaceName.Default, eventSourceId));
}
