// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Orleans.Chronicle.Integration;

public static class EventContexts
{
    public const string EventStore = "integration";

    public static EventContext For<TEvent>(EventStoreNamespaceName @namespace, EventSourceId eventSourceId, ulong sequenceNumber) =>
        EventContext.From(
            EventStore,
            @namespace,
            typeof(TEvent).GetEventType(),
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.New());
}
