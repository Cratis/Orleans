// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when a <see cref="StateMachineKey"/> cannot be formed or parsed.
/// </summary>
public class InvalidStateMachineKey : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidStateMachineKey"/> class, for a key that cannot be parsed.
    /// </summary>
    /// <param name="key">The key that could not be parsed.</param>
    public InvalidStateMachineKey(string key)
        : base($"'{key}' is not a state machine key - expected '{{eventStore}}{StateMachineKey.Separator}{{namespace}}{StateMachineKey.Separator}{{eventSourceId}}'")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidStateMachineKey"/> class, for an event store or namespace
    /// that cannot be part of a key.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    public InvalidStateMachineKey(EventStoreName eventStore, EventStoreNamespaceName @namespace)
        : base($"The event store '{eventStore}' and namespace '{@namespace}' cannot form a state machine key - neither may contain '{StateMachineKey.Separator}'")
    {
    }
}
