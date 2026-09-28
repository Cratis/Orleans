// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents the fluent reactor that delivers the events an event-sourced state machine is interested in to the state
/// machine grain for the event's event source.
/// </summary>
/// <param name="definition">The <see cref="EventSourcedStateMachineDefinition"/> of the state machine.</param>
/// <param name="grains">Provides the <see cref="IStateMachineGrainResolver"/>, resolved on first delivery.</param>
/// <param name="eventStores">Provides the <see cref="IStateMachineEventStores"/>, resolved on first delivery.</param>
/// <param name="options">The <see cref="EventSourcedStateMachinesOptions"/>.</param>
/// <remarks>
/// <para>
/// The reactor subscribes to exactly the event types in <see cref="EventSourcedStateMachineDefinition.EventTypes"/>, one
/// typed handler each - never to all events. Chronicle registers it for every event store and namespace the client
/// creates, and every delivered event carries its event store and namespace, so the grain it refreshes is always the
/// one for the same event store, namespace and event source.
/// </para>
/// <para>
/// A materialized model is written by its projection's own observer, which may not have processed the delivered event
/// yet. Before refreshing, the reactor waits for the projection to reach the event's sequence number when the event is
/// one the projection consumes, and fails the delivery if it does not within the configured time - Chronicle then
/// retries the partition, so a stale model never decides a transition. Events are delivered to the reactor in order for
/// each event source, so once a projected event has been waited for, a later transition-only event sees a model that
/// includes it. A passive model is computed from the event log when it is read, which already includes the delivered
/// event, so it never waits.
/// </para>
/// </remarks>
internal sealed class EventSourcedStateMachineReactor(
    EventSourcedStateMachineDefinition definition,
    Func<IStateMachineGrainResolver> grains,
    Func<IStateMachineEventStores> eventStores,
    EventSourcedStateMachinesOptions options)
{
    /// <summary>
    /// Define the reactor: one typed handler for each event type the state machine is interested in.
    /// </summary>
    /// <param name="builder">The <see cref="IReactorBuilder"/>.</param>
    public void Define(IReactorBuilder builder)
    {
        foreach (var eventType in definition.EventTypes)
        {
            builder.On(eventType, Handle);
        }
    }

    /// <summary>
    /// Handle a delivered event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(object @event, EventContext context)
    {
        var eventType = @event.GetType();
        if (!definition.IsPassive && definition.ProjectionEventTypes.Contains(eventType))
        {
            await WaitForModelToCatchUpWith(context);
        }

        var grain = grains().Get(definition.StateMachineType, StateMachineKey.From(context));
        await grain.Refresh(StateMachineRefresh.For(eventType, context));
    }

    async Task WaitForModelToCatchUpWith(EventContext context)
    {
        var eventStore = await eventStores().Get(context.EventStore, context.Namespace);
        using var timeout = new CancellationTokenSource(options.ModelCatchUpTimeout);
        while (true)
        {
            var state = await eventStore.Projections.GetStateForModel(definition.ModelType);
            if (state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber.Value >= context.SequenceNumber.Value)
            {
                return;
            }

            if (timeout.IsCancellationRequested)
            {
                throw new ModelDidNotCatchUp(definition.ModelType, context.SequenceNumber, options.ModelCatchUpTimeout);
            }

            await Task.Delay(options.ModelCatchUpPollInterval, timeout.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
