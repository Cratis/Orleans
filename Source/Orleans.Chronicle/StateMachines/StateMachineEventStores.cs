// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IStateMachineEventStores"/> that asks the <see cref="IChronicleClient"/>,
/// which keeps one <see cref="IEventStore"/> per event store and namespace.
/// </summary>
/// <param name="client">The <see cref="IChronicleClient"/>.</param>
public class StateMachineEventStores(IChronicleClient client) : IStateMachineEventStores
{
    /// <inheritdoc/>
    public Task<IEventStore> Get(EventStoreName eventStore, EventStoreNamespaceName @namespace) => client.GetEventStore(eventStore, @namespace);
}
