// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Orleans.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents a <see cref="StateMachine{TStoredState}"/> whose model is a Chronicle read model derived from events, and
/// whose transitions are driven by those events.
/// </summary>
/// <typeparam name="TModel">Type of read model the state machine follows.</typeparam>
/// <remarks>
/// <para>
/// Chronicle events are the only source of truth. The model is a read model Chronicle projects from them - declared
/// with model-bound projection attributes on <typeparamref name="TModel"/>, or in <see cref="DefineProjection"/> - and
/// the state machine only ever reads it. Nothing is persisted by the grain: its storage writes nothing, and the current
/// state is not recorded anywhere but derived from the model by <see cref="ResolveState"/> whenever the grain activates.
/// No storage provider needs to be configured.
/// </para>
/// <para>
/// The grain key is a <see cref="StateMachineKey"/> - the event store, namespace and event source the state machine
/// follows. When an event the state machine is interested in is appended, the integration activates the grain for its
/// event source, the grain re-reads the model, <see cref="OnModelChanged"/> is called, and the transitions declared in
/// <see cref="DefineTransitions"/> for the event are evaluated against the refreshed model.
/// </para>
/// <para>
/// <see cref="DefineProjection"/> and <see cref="DefineTransitions"/> are called at startup - on an instance created
/// without running any constructor, to learn which events the state machine needs - and again by each activation. They
/// must describe the state machine from its type alone: they must not use fields, constructor parameters or injected
/// services.
/// </para>
/// </remarks>
public abstract class EventSourcedStateMachine<TModel> : StateMachine<TModel>, IEventSourcedStateMachine
    where TModel : class
{
    readonly ReadModelStorage<TModel> _storage;
    StateMachineTransitions<TModel>? _transitions;
    ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcedStateMachine{TModel}"/> class.
    /// </summary>
    protected EventSourcedStateMachine()
        : this(new ReadModelStorage<TModel>())
    {
    }

    EventSourcedStateMachine(ReadModelStorage<TModel> storage)
        : base(storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Gets the declared transitions.
    /// </summary>
    internal StateMachineTransitions<TModel> Transitions => _transitions ??= DescribeTransitions();

    /// <summary>
    /// Gets the <see cref="StateMachineKey"/> of the state machine - the event source it follows and where its events live.
    /// </summary>
    protected StateMachineKey Key => _storage.Key;

    /// <summary>
    /// Gets the model as it currently is, or null while it has no instance.
    /// </summary>
    protected TModel? Model => State;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
        _storage.Bind(ServiceProvider, StateMachineKey.Parse(this.GetPrimaryKeyString()));
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Refresh(StateMachineRefresh refresh)
    {
        var context = refresh.GetContext();
        var previous = State;
        await ReadStateAsync();
        var current = State;

        await OnModelChanged(previous, current, context);
        await ApplyTransitionsFor(refresh.EventType);
    }

    /// <summary>
    /// Check whether a state machine type describes its projection in code.
    /// </summary>
    /// <param name="stateMachineType">The type of state machine.</param>
    /// <returns>True if it overrides <see cref="DefineProjection"/>, false if not.</returns>
    internal static bool DefinesProjection(Type stateMachineType)
    {
        var method = stateMachineType.GetMethod(
            nameof(DefineProjection),
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(IProjectionBuilderFor<TModel>)])!;
        return method.DeclaringType != typeof(EventSourcedStateMachine<TModel>);
    }

    /// <summary>
    /// Describe the projection of the state machine on a builder.
    /// </summary>
    /// <param name="projection">The <see cref="IProjectionBuilderFor{TReadModel}"/> to describe it on.</param>
    internal void DescribeProjection(IProjectionBuilderFor<TModel> projection) => DefineProjection(projection);

    /// <summary>
    /// Describe the transitions of the state machine.
    /// </summary>
    /// <returns>The declared <see cref="StateMachineTransitions{TModel}"/>.</returns>
    internal StateMachineTransitions<TModel> DescribeTransitions()
    {
        var transitions = new StateMachineTransitions<TModel>();
        DefineTransitions(transitions);
        return transitions;
    }

    /// <summary>
    /// Describe the projection that maintains the model, when it is not declared by model-bound projection attributes on
    /// <typeparamref name="TModel"/>.
    /// </summary>
    /// <param name="projection">The <see cref="IProjectionBuilderFor{TReadModel}"/> to describe the projection on.</param>
    /// <remarks>
    /// Overriding this makes the integration register the projection with Chronicle. Leave it alone to use the
    /// model-bound projection attributes on <typeparamref name="TModel"/>, or a projection for it Chronicle already has.
    /// Calling <see cref="IProjectionBuilderFor{TReadModel}.Passive"/> is recommended: a passive model is computed from
    /// the event log on demand, so it always reflects the event that was just delivered.
    /// </remarks>
    protected virtual void DefineProjection(IProjectionBuilderFor<TModel> projection)
    {
    }

    /// <summary>
    /// Declare the transitions taken when events are delivered.
    /// </summary>
    /// <param name="transitions">The <see cref="IStateMachineTransitions{TModel}"/> to declare the transitions on.</param>
    /// <remarks>
    /// For a delivered event, the transitions declared for its type are evaluated in the order they are declared, against
    /// the model as it is after the event. The first one whose guards are satisfied and that the current state allows is
    /// taken; the rest are not evaluated.
    /// </remarks>
    protected virtual void DefineTransitions(IStateMachineTransitions<TModel> transitions)
    {
    }

    /// <summary>
    /// Resolve the state the state machine is in from its model.
    /// </summary>
    /// <param name="model">The model, or null while it has no instance.</param>
    /// <returns>The type of state the model says the state machine is in.</returns>
    /// <remarks>
    /// This is how the state machine finds its state when it activates - the state is never persisted, so it is always
    /// what the authoritative model says. The default is <see cref="StateMachine{TStoredState}.InitialState"/>.
    /// </remarks>
    protected virtual Type ResolveState(TModel? model) => InitialState;

    /// <summary>
    /// Called when a delivered event has been applied to the model, before any transition for the event is evaluated.
    /// </summary>
    /// <param name="previous">The model as it was before the event, or null when it had no instance.</param>
    /// <param name="current">The model as it is after the event, or null when it has no instance.</param>
    /// <param name="context">The <see cref="EventContext"/> of the delivered event.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnModelChanged(TModel? previous, TModel? current, EventContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    protected sealed override Type ResolveActivationState() => ResolveState(State);

    async Task ApplyTransitionsFor(Type eventType)
    {
        foreach (var transition in Transitions.For(eventType))
        {
            if (!transition.IsAllowedFor(State))
            {
                _logger?.GuardNotSatisfied(eventType, transition.TargetState);
                continue;
            }

            if (!await CanTransitionTo(transition.TargetState))
            {
                _logger?.CurrentStateDoesNotAllowTransition(eventType, transition.TargetState);
                continue;
            }

            await TransitionTo(transition.TargetState);
            return;
        }
    }
}
