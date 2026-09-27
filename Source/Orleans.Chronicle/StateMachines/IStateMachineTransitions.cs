// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines the builder an <see cref="EventSourcedStateMachine{TModel}"/> declares its event-driven transitions on.
/// </summary>
/// <typeparam name="TModel">Type of model the state machine follows.</typeparam>
public interface IStateMachineTransitions<TModel>
    where TModel : class
{
    /// <summary>
    /// Start declaring a transition taken when an event of a specific type is delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of event. An interface or base type matches every event type implementing it.</typeparam>
    /// <returns>The <see cref="IStateMachineTransitionBuilder{TModel}"/> to complete the transition on.</returns>
    /// <exception cref="TransitionEventIsNotAnEventType">Thrown when <typeparamref name="TEvent"/> is not a Chronicle event type or a type event types implement.</exception>
    IStateMachineTransitionBuilder<TModel> On<TEvent>();
}
