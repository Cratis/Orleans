// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents the identity of an event-sourced state machine grain: the event source it follows, in the event store and
/// namespace its events live in.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> the events live in.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the events live in - typically the tenant.</param>
/// <param name="EventSourceId">The <see cref="Cratis.Chronicle.Events.EventSourceId"/> of the events, which is also the key of the model.</param>
/// <remarks>
/// <para>
/// The string form - the grain key - is <c language="csharp">{eventStore}/{namespace}/{eventSourceId}</c>. The event store
/// and namespace are part of it so that the same event source in two namespaces, or two event stores, is two different
/// grains reading two different models - a grain keyed on the event source id alone would serve one tenant's model to
/// another. The event source id is the last part and may itself contain the separator; the event store name and the
/// namespace may not.
/// </para>
/// </remarks>
public record StateMachineKey(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSourceId EventSourceId)
{
    /// <summary>
    /// The separator between the parts of the key.
    /// </summary>
    public const char Separator = '/';

    /// <summary>
    /// Create a <see cref="StateMachineKey"/> for the event source an event was appended to.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>The <see cref="StateMachineKey"/> of the state machine following the event source.</returns>
    public static StateMachineKey From(EventContext context) => new(context.EventStore, context.Namespace, context.EventSourceId);

    /// <summary>
    /// Create a <see cref="StateMachineKey"/> for an event source in the event store and namespace of an <see cref="IEventStore"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the events live in.</param>
    /// <param name="eventSourceId">The <see cref="Cratis.Chronicle.Events.EventSourceId"/> to follow.</param>
    /// <returns>The <see cref="StateMachineKey"/>.</returns>
    public static StateMachineKey From(IEventStore eventStore, EventSourceId eventSourceId) => new(eventStore.Name, eventStore.Namespace, eventSourceId);

    /// <summary>
    /// Parse the string form of a <see cref="StateMachineKey"/>.
    /// </summary>
    /// <param name="key">The string form, as produced by <see cref="ToString"/>.</param>
    /// <returns>The parsed <see cref="StateMachineKey"/>.</returns>
    /// <exception cref="InvalidStateMachineKey">Thrown when the key does not have the three parts of a state machine key.</exception>
    public static StateMachineKey Parse(string key)
    {
        var eventStoreEnd = key.IndexOf(Separator, StringComparison.Ordinal);
        var namespaceEnd = eventStoreEnd < 0 ? -1 : key.IndexOf(Separator, eventStoreEnd + 1);
        if (eventStoreEnd <= 0 || namespaceEnd <= eventStoreEnd + 1 || namespaceEnd == key.Length - 1)
        {
            throw new InvalidStateMachineKey(key);
        }

        return new(
            key[..eventStoreEnd],
            key[(eventStoreEnd + 1)..namespaceEnd],
            new EventSourceId(key[(namespaceEnd + 1)..]));
    }

    /// <summary>
    /// Get the string form of the key, which is the grain key of the state machine.
    /// </summary>
    /// <returns>The string form of the key.</returns>
    /// <exception cref="InvalidStateMachineKey">Thrown when the event store name or the namespace contains the separator.</exception>
    public override string ToString()
    {
        if (EventStore.Value.Contains(Separator, StringComparison.Ordinal) || Namespace.Value.Contains(Separator, StringComparison.Ordinal))
        {
            throw new InvalidStateMachineKey(EventStore, Namespace);
        }

        return $"{EventStore.Value}{Separator}{Namespace.Value}{Separator}{EventSourceId.Value}";
    }
}
