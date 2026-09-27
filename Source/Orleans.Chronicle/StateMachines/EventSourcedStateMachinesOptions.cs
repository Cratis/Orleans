// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents the options for event-sourced state machines.
/// </summary>
public class EventSourcedStateMachinesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether every concrete <see cref="EventSourcedStateMachine{TModel}"/> is
    /// discovered by type discovery. Defaults to true.
    /// </summary>
    public bool Discover { get; set; } = true;

    /// <summary>
    /// Gets the state machine types to register in addition to the discovered ones.
    /// </summary>
    public ISet<Type> StateMachines { get; } = new HashSet<Type>();

    /// <summary>
    /// Gets or sets how long a delivered event waits for a materialized model to catch up with it before its delivery
    /// fails. Defaults to 30 seconds. A passive model never waits.
    /// </summary>
    public TimeSpan ModelCatchUpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how often a materialized model is checked while waiting for it to catch up. Defaults to 50 milliseconds.
    /// </summary>
    public TimeSpan ModelCatchUpPollInterval { get; set; } = TimeSpan.FromMilliseconds(50);
}
