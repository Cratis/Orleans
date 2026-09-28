// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines the builder for completing one event-driven transition of an <see cref="EventSourcedStateMachine{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model the state machine follows.</typeparam>
public interface IStateMachineTransitionBuilder<TModel>
    where TModel : class
{
    /// <summary>
    /// Only take the transition when the refreshed model satisfies a guard.
    /// </summary>
    /// <param name="guard">The guard, given the model as it is after the event. A transition with a guard is never taken while the model has no instance.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>Calling it more than once requires every guard to be satisfied.</remarks>
    IStateMachineTransitionBuilder<TModel> When(Func<TModel, bool> guard);

    /// <summary>
    /// Complete the transition, naming the state it moves the state machine to.
    /// </summary>
    /// <typeparam name="TState">Type of state to transition to.</typeparam>
    /// <returns>The <see cref="IStateMachineTransitions{TModel}"/> for declaring more transitions.</returns>
    IStateMachineTransitions<TModel> TransitionTo<TState>()
        where TState : IState<TModel>;
}
