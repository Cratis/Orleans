// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents one event-driven transition of an <see cref="EventSourcedStateMachine{TModel}"/>.
/// </summary>
/// <param name="EventType">The type of event the transition is taken for. An interface or base type matches every event type implementing it.</param>
/// <param name="TargetState">The type of state the transition moves to.</param>
/// <param name="Guards">The guards the refreshed model must satisfy for the transition to be taken.</param>
/// <typeparam name="TModel">Type of model the state machine follows.</typeparam>
public record StateMachineTransition<TModel>(Type EventType, Type TargetState, IReadOnlyList<Func<TModel, bool>> Guards)
    where TModel : class
{
    /// <summary>
    /// Gets a value indicating whether the transition has guards.
    /// </summary>
    public bool IsGuarded => Guards.Count > 0;

    /// <summary>
    /// Check whether the transition applies to a delivered event.
    /// </summary>
    /// <param name="eventType">The type of the delivered event.</param>
    /// <returns>True if it applies, false if not.</returns>
    public bool AppliesTo(Type eventType) => EventType.IsAssignableFrom(eventType);

    /// <summary>
    /// Check whether the guards allow the transition for a model.
    /// </summary>
    /// <param name="model">The model as it is after the event, or null when it has no instance.</param>
    /// <returns>True if every guard is satisfied - which a model without an instance only does when there are none.</returns>
    public bool IsAllowedFor(TModel? model) =>
        !IsGuarded || (model is not null && Guards.All(guard => guard(model)));
}
