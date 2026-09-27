// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IStateMachineTransitions{TModel}"/> that collects the declared
/// transitions so they can be inspected - at startup for the events they need, and on delivery for the one to take.
/// </summary>
/// <typeparam name="TModel">Type of model the state machine follows.</typeparam>
public class StateMachineTransitions<TModel> : IStateMachineTransitions<TModel>
    where TModel : class
{
    readonly List<StateMachineTransition<TModel>> _transitions = [];

    /// <summary>
    /// Gets the declared transitions, in the order they were declared.
    /// </summary>
    public IImmutableList<StateMachineTransition<TModel>> Transitions => [.. _transitions];

    /// <summary>
    /// Gets the types of event the transitions are declared for.
    /// </summary>
    public IEnumerable<Type> EventTypes => _transitions.Select(_ => _.EventType).Distinct();

    /// <inheritdoc/>
    public IStateMachineTransitionBuilder<TModel> On<TEvent>()
    {
        var eventType = typeof(TEvent);
        if (!Attribute.IsDefined(eventType, typeof(EventTypeAttribute)) && !eventType.IsInterface && !eventType.IsAbstract)
        {
            throw new TransitionEventIsNotAnEventType(eventType);
        }

        return new Builder(this, eventType);
    }

    /// <summary>
    /// Get the transitions that apply to a delivered event, in the order they were declared.
    /// </summary>
    /// <param name="eventType">The type of the delivered event.</param>
    /// <returns>The transitions that apply.</returns>
    public IEnumerable<StateMachineTransition<TModel>> For(Type eventType) => _transitions.Where(_ => _.AppliesTo(eventType));

    sealed class Builder(StateMachineTransitions<TModel> transitions, Type eventType) : IStateMachineTransitionBuilder<TModel>
    {
        readonly List<Func<TModel, bool>> _guards = [];

        public IStateMachineTransitionBuilder<TModel> When(Func<TModel, bool> guard)
        {
            ArgumentNullException.ThrowIfNull(guard);
            _guards.Add(guard);
            return this;
        }

        public IStateMachineTransitions<TModel> TransitionTo<TState>()
            where TState : IState<TModel>
        {
            transitions._transitions.Add(new(eventType, typeof(TState), [.. _guards]));
            return transitions;
        }
    }
}
