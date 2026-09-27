// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Holds a substitute event store per namespace behind a substitute <see cref="IStateMachineEventStores"/>.
/// </summary>
public class EventStoresForTesting
{
    readonly Dictionary<string, IEventStore> _eventStores = [];

    public EventStoresForTesting()
    {
        Provider = Substitute.For<IStateMachineEventStores>();
        Provider.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(call => Task.FromResult(For(call.Arg<EventStoreNamespaceName>().Value)));
    }

    public IStateMachineEventStores Provider { get; }

    public IEventStore For(string @namespace)
    {
        if (!_eventStores.TryGetValue(@namespace, out var eventStore))
        {
            eventStore = Substitute.For<IEventStore>();
            eventStore.Name.Returns(new EventStoreName("some-store"));
            eventStore.Namespace.Returns(new EventStoreNamespaceName(@namespace));
            eventStore.ReadModels.Returns(Substitute.For<IReadModels>());
            eventStore.Projections.Returns(Substitute.For<IProjections>());
            _eventStores[@namespace] = eventStore;
        }

        return eventStore;
    }
}
