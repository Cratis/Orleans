// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines how event-sourced state machines reach the <see cref="IEventStore"/> for an event store and namespace.
/// </summary>
/// <remarks>
/// A grain runs outside of any request, so it cannot use a scoped, tenant-resolved <see cref="IEventStore"/>. It names
/// the event store and namespace from its own key instead, which is what keeps one tenant's state machine from ever
/// reading another tenant's model.
/// </remarks>
public interface IStateMachineEventStores
{
    /// <summary>
    /// Get the <see cref="IEventStore"/> for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <returns>The <see cref="IEventStore"/>.</returns>
    Task<IEventStore> Get(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
