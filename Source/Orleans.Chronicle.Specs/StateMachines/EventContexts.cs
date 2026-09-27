// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Orleans.Chronicle.StateMachines;

public static class EventContexts
{
    public static EventContext For<TEvent>(EventStoreNamespaceName @namespace, EventSourceId eventSourceId, ulong sequenceNumber = 42) =>
        EventContext.From(
            "some-store",
            @namespace,
            typeof(TEvent).GetEventType(),
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.New());
}
