// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines the grain interface every <see cref="EventSourcedStateMachine{TModel}"/> implements, through which delivered
/// events reach it.
/// </summary>
/// <remarks>
/// A state machine's own grain interface derives from this. The integration calls <see cref="Refresh"/>; application
/// code talks to the state machine through its own interface.
/// </remarks>
public interface IEventSourcedStateMachine : IGrainWithStringKey
{
    /// <summary>
    /// Refresh the state machine for a delivered event: re-read the model and apply the transitions for the event.
    /// </summary>
    /// <param name="refresh">The <see cref="StateMachineRefresh"/> describing the delivered event.</param>
    /// <returns>Awaitable task.</returns>
    Task Refresh(StateMachineRefresh refresh);
}
