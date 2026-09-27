// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// Represents a base implementation of <see cref="IStateMachine{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public abstract class StateMachine<TStoredState> : Grain<TStoredState>, IStateMachine<TStoredState>
{
    static readonly NoOpState<TStoredState> _noOpState = new();
    static readonly MethodInfo _canTransitionToMethod = typeof(IState<TStoredState>).GetMethod(nameof(IState<TStoredState>.CanTransitionTo))!;

    readonly bool _hasSuppliedStorage;
    Dictionary<Type, IState<TStoredState>> _states = [];
    IState<TStoredState> _currentState = _noOpState;
    bool _isTransitioning;
    bool _isLeaving;
    Type? _scheduledTransition;
    ILogger<StateMachine<TStoredState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachine{TStoredState}"/> class, with the stored state
    /// coming from the grain storage configured for the grain, typically through a
    /// <see cref="global::Orleans.Providers.StorageProviderAttribute"/>.
    /// </summary>
    protected StateMachine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachine{TStoredState}"/> class, with the stored state
    /// coming from the supplied <see cref="IStorage{TStoredState}"/> rather than a configured grain storage provider.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage{TStoredState}"/> holding the stored state.</param>
    /// <remarks>
    /// Orleans does not read supplied storage as part of the grain lifecycle, so the state machine reads it on
    /// activation, before <see cref="OnActivation"/> is called and the current state is resolved.
    /// </remarks>
    protected StateMachine(IStorage<TStoredState> storage)
        : base(storage)
    {
        _hasSuppliedStorage = true;
    }

    /// <summary>
    /// Gets whether or not the state machine is in a state.
    /// </summary>
    public bool IsInActiveState => _currentState.GetType() != _noOpState.GetType();

    /// <summary>
    /// Gets the initial state of the state machine.
    /// </summary>
    /// <returns>Type of initial state.</returns>
    protected virtual Type InitialState => typeof(NoOpState<TStoredState>);

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<StateMachine<TStoredState>>>() ?? NullLogger<StateMachine<TStoredState>>.Instance;

        if (_hasSuppliedStorage)
        {
            await ReadStateAsync();
        }

        await OnActivation(cancellationToken);
        _states = CreateStates().ToDictionary(_ => _.GetType());
        _states[typeof(NoOpState<TStoredState>)] = new NoOpState<TStoredState>();
        foreach (var state in _states.Values.OfType<State<TStoredState>>())
        {
            state.StateMachine = this;
        }

        var activationState = ResolveActivationState();

        InvalidTypeForState.ThrowIfInvalid(activationState);
        ThrowIfUnknownStateType(activationState);
        await PerformTransition(activationState);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        State = await _currentState.OnLeave(State);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IState<TStoredState>> GetCurrentState() => Task.FromResult(_currentState);

    /// <inheritdoc/>
    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState> => _currentState.CanTransitionTo<TState>(State);

    /// <inheritdoc/>
    public async Task TransitionTo<TState>()
        where TState : IState<TStoredState>
    {
        ThrowIfUnknownStateType(typeof(TState));
        if (await CanTransitionTo<TState>())
        {
            await PerformTransition(typeof(TState));
        }
    }

    /// <inheritdoc/>
    public Task<IImmutableList<IState<TStoredState>>> GetStates() => Task.FromResult<IImmutableList<IState<TStoredState>>>(_states.Values.ToImmutableList());

    /// <summary>
    /// Gets the states for this state machine.
    /// </summary>
    /// <returns>A collection of states.</returns>
    public abstract IImmutableList<IState<TStoredState>> CreateStates();

    /// <summary>
    /// Called when the state machine is activating.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for any cancellations.</param>
    /// <returns>Awaitable task.</returns>
    public virtual Task OnActivation(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Check if the current state allows a transition to a specific state, identified by its type.
    /// </summary>
    /// <param name="stateType">Type of state to check.</param>
    /// <returns>True if it can, false if not.</returns>
    /// <remarks>
    /// This asks the current state exactly as <see cref="CanTransitionTo{TState}"/> does, by calling
    /// <see cref="IState{TStoredState}.CanTransitionTo{TTargetState}(TStoredState)"/> with <paramref name="stateType"/>
    /// as the target state type. Use it when the target state is only known as a <see cref="Type"/>.
    /// </remarks>
    /// <exception cref="InvalidTypeForState">Thrown when <paramref name="stateType"/> is not a state for this state machine.</exception>
    protected Task<bool> CanTransitionTo(Type stateType)
    {
        ThrowIfNotAStateType(stateType);
        return (Task<bool>)_canTransitionToMethod
            .MakeGenericMethod(stateType)
            .Invoke(_currentState, BindingFlags.DoNotWrapExceptions, binder: null, [State], culture: null)!;
    }

    /// <summary>
    /// Transition to a specific state, identified by its type, if the current state allows it.
    /// </summary>
    /// <param name="stateType">Type of state to transition to.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This behaves exactly as <see cref="TransitionTo{TState}"/>: the transition only happens when the current state
    /// allows it, and a transition requested while entering a state is performed once the ongoing transition completes.
    /// Use it when the target state is only known as a <see cref="Type"/>.
    /// </remarks>
    /// <exception cref="InvalidTypeForState">Thrown when <paramref name="stateType"/> is not a state for this state machine.</exception>
    /// <exception cref="UnknownStateTypeInStateMachine">Thrown when <paramref name="stateType"/> is not one of the states of this state machine.</exception>
    protected async Task TransitionTo(Type stateType)
    {
        ThrowIfNotAStateType(stateType);
        ThrowIfUnknownStateType(stateType);
        if (await CanTransitionTo(stateType))
        {
            await PerformTransition(stateType);
        }
    }

    /// <summary>
    /// Resolve the state to enter when the state machine activates.
    /// </summary>
    /// <returns>Type of state to enter.</returns>
    /// <remarks>
    /// By default, this is the state recorded in <see cref="StateMachineState.CurrentState"/> when the stored state is a
    /// <see cref="StateMachineState"/> with a current state recorded, and <see cref="InitialState"/> otherwise. Override it
    /// to derive the state from somewhere else, such as an authoritative model held in the stored state. It is called
    /// after <see cref="OnActivation"/> and after the states have been created.
    /// </remarks>
    /// <exception cref="UnknownCurrentState">Thrown when the recorded current state is not one of the states of this state machine.</exception>
    protected virtual Type ResolveActivationState()
    {
        if (State is StateMachineState stateMachineState && !string.IsNullOrEmpty(stateMachineState.CurrentState))
        {
            var state = _states.Values.FirstOrDefault(_ => _.GetType().FullName == stateMachineState.CurrentState) ?? throw new UnknownCurrentState(stateMachineState.CurrentState, GetType());
            return state.GetType();
        }

        return InitialState;
    }

    /// <summary>
    /// Method that gets called before entering a state.
    /// </summary>
    /// <param name="state">Instance of the state that will be entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnBeforeEnteringState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called after entering a state.
    /// </summary>
    /// <param name="state">Instance of the state that was entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnAfterEnteringState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called before leaving a state.
    /// </summary>
    /// <param name="state">Instance of the state that will be entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnBeforeLeavingState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called after leaving a state.
    /// </summary>
    /// <param name="state">Instance of the state that was entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnAfterLeavingState(IState<TStoredState> state) => Task.CompletedTask;

    async Task PerformTransition(Type stateType)
    {
        _logger?.TransitioningTo(stateType);
        if (_isTransitioning)
        {
            if (_isLeaving)
            {
                throw new TransitioningDuringOnLeaveIsNotSupported(_currentState.GetType(), stateType, GetType());
            }
            _scheduledTransition = stateType;
            return;
        }

        var stateTypeBeforeTransition = _currentState.GetType();
        var storedStateBeforeTransition = State;

        _isTransitioning = true;
        _isLeaving = true;

        // OnLeave and OnEnter run arbitrary state code that does I/O, so either can throw. The in-transition flags
        // must come down regardless: a stuck _isTransitioning turns every later transition into a scheduled one that
        // never runs, and a stuck _isLeaving makes every later transition throw
        // TransitioningDuringOnLeaveIsNotSupported - either way the machine can never change state again, and being
        // kept alive it is never reactivated out of it. A transition scheduled from inside a transition that then
        // failed is dropped with it; it was an intent of a transition that never completed, and re-running it later
        // on the back of an unrelated one would be arbitrary.
        try
        {
            await OnBeforeLeavingState(_currentState);
            State = await _currentState.OnLeave(State);
            await OnAfterLeavingState(_currentState);
            _isLeaving = false;

            _currentState = _states[stateType];

            await OnBeforeEnteringState(_currentState);
            State = await _currentState.OnEnter(State);
            await OnAfterEnteringState(_currentState);

            if (State is StateMachineState stateMachineState)
            {
                stateMachineState.CurrentState = _currentState.GetType().FullName!;
            }

            // A transition that neither moved to a different state nor produced a new stored-state instance leaves the
            // persisted document identical, so the write is skipped. The current-state type change captures the
            // StateMachineState.CurrentState update above; a new State reference captures every OnLeave/OnEnter result.
            if (_currentState.GetType() != stateTypeBeforeTransition || !ReferenceEquals(State, storedStateBeforeTransition))
            {
                await WriteStateAsync();
            }
        }
        catch
        {
            _scheduledTransition = null;
            throw;
        }
        finally
        {
            _isLeaving = false;
            _isTransitioning = false;
        }

        if (_scheduledTransition is not null)
        {
            var stateToTransitionTo = _scheduledTransition;
            _scheduledTransition = null;
            await PerformTransition(stateToTransitionTo);
        }
    }

    void ThrowIfNotAStateType(Type type)
    {
        if (!typeof(IState<TStoredState>).IsAssignableFrom(type))
        {
            throw new InvalidTypeForState(type);
        }
    }

    void ThrowIfUnknownStateType(Type type)
    {
        if (!_states.ContainsKey(type))
        {
            throw new UnknownStateTypeInStateMachine(type, GetType());
        }
    }
}
