// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration;

/// <summary>
/// Stands in for the read models Chronicle projects, per event store namespace - what a projection has made of the
/// events so far is set by the spec, exactly as a passive projection would compute it when read.
/// </summary>
public class InMemoryReadModels : IStateMachineEventStores
{
    readonly ConcurrentDictionary<(string Namespace, string Key), object> _models = new();
    readonly ConcurrentDictionary<string, IEventStore> _eventStores = new();

    public static InMemoryReadModels Instance { get; } = new();

    public void Set(string @namespace, string key, object model) => _models[(@namespace, key)] = model;

    public void Clear() => _models.Clear();

    public Task<IEventStore> Get(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Task.FromResult(_eventStores.GetOrAdd(@namespace.Value, CreateEventStore));

    IEventStore CreateEventStore(string @namespace)
    {
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById<Documents.Document>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(call => Task.FromResult(_models.TryGetValue((@namespace, call.Arg<ReadModelKey>().Value), out var model) ? (Documents.Document)model : null!));

        var eventStore = Substitute.For<IEventStore>();
        eventStore.Namespace.Returns(new EventStoreNamespaceName(@namespace));
        eventStore.ReadModels.Returns(readModels);
        return eventStore;
    }
}
