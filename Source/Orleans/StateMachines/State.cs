// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IState{TStoredState}"/>.
/// </summary>
/// <remarks>
/// The owning <see cref="StateMachine{TStoredState}"/> sets <see cref="StateMachine"/> on every state deriving from
/// this type when it activates, regardless of which assembly the state is defined in. For testing a state in isolation,
/// use <see cref="StateExtensions.SetStateMachine{TStoredState}(State{TStoredState}, IStateMachine{TStoredState})"/>.
/// </remarks>
/// <typeparam name="TStoredState">Type of state object associated.</typeparam>
public class State<TStoredState> : IState<TStoredState>
{
    /// <inheritdoc/>
    public IStateMachine<TStoredState> StateMachine { get; internal set; } = default!;

    /// <summary>
    /// Gets the supported state transitions from this state.
    /// </summary>
    protected virtual IImmutableList<Type> AllowedTransitions => ImmutableList<Type>.Empty;

    /// <inheritdoc/>
    public virtual Task<bool> CanTransitionTo<TTargetState>(TStoredState state) => Task.FromResult(AllowedTransitions.Contains(typeof(TTargetState)));

    /// <inheritdoc/>
    public virtual Task<TStoredState> OnEnter(TStoredState state) => Task.FromResult(state);

    /// <inheritdoc/>
    public virtual Task<TStoredState> OnLeave(TStoredState state) => Task.FromResult(state);
}
