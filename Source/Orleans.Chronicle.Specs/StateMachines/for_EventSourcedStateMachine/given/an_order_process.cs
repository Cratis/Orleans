// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Orleans.Chronicle.StateMachines.Orders;
using Orleans.TestKit;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.given;

public abstract class an_order_process : Specification
{
    protected const string Key = "some-store/tenant-a/order-1";

    protected TestKitSilo _silo;
    protected EventStoresForTesting _eventStores;
    protected Queue<Order?> _models;
    protected OrderProcess _stateMachine;

    void Establish()
    {
        _silo = new();
        _eventStores = new();
        _silo.AddService(_eventStores.Provider);
        _models = new();
        _eventStores.For("tenant-a").ReadModels
            .GetInstanceById<Order>(Arg.Any<ReadModelKey>())
            .Returns(_ => (_models.Count > 1 ? _models.Dequeue() : _models.Peek())!);
    }

    protected async Task Activate() => _stateMachine = await _silo.CreateGrainAsync<OrderProcess>(Key);

    protected Task Deliver<TEvent>(TEvent @event, ulong sequenceNumber = 42)
        where TEvent : notnull =>
        _stateMachine.Refresh(StateMachineRefresh.For(@event.GetType(), EventContexts.For<TEvent>("tenant-a", "order-1", sequenceNumber)));

    protected async Task<Type> CurrentState() => (await _stateMachine.GetCurrentState()).GetType();

    protected (Order? Previous, Order? Current, EventContext Context, Type StateWhenCalled) OnlyModelChange => _stateMachine.ModelChanges.Single();
}
