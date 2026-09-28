// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration;

/// <summary>
/// Records what the integration registers with Chronicle, and plays the part of Chronicle delivering events to the
/// reactors it registered.
/// </summary>
public class RecordingRegistrations : IStateMachineArtifactRegistrations
{
    readonly Dictionary<Type, Func<object, EventContext, Task>> _handlers = [];

    public List<Type> RegisteredProjections { get; } = [];

    public List<ReactorId> RegisteredReactors { get; } = [];

    public IEnumerable<Type> SubscribedEventTypes => _handlers.Keys;

    public void RegisterProjection<TModel>(Action<IProjectionBuilderFor<TModel>> define) => RegisteredProjections.Add(typeof(TModel));

    public void RegisterReadModel<TModel>() => RegisteredProjections.Add(typeof(TModel));

    public void RegisterReactor(ReactorId id, Action<IReactorBuilder> define)
    {
        RegisteredReactors.Add(id);
        define(new Builder(_handlers));
    }

    public Task Deliver(object @event, EventContext context) => _handlers[@event.GetType()](@event, context);

    sealed class Builder(Dictionary<Type, Func<object, EventContext, Task>> handlers) : IReactorBuilder
    {
        public IReactorBuilder On<TEvent>(Func<TEvent, EventContext, Task> handler)
        {
            handlers.Add(typeof(TEvent), (@event, context) => handler((TEvent)@event, context));
            return this;
        }

        public IReactorBuilder On<TEvent>(Action<TEvent> handler) => throw new NotSupportedException();

        public IReactorBuilder On<TEvent>(Action<TEvent, EventContext> handler) => throw new NotSupportedException();

        public IReactorBuilder On<TEvent>(Func<TEvent, Task> handler) => throw new NotSupportedException();

        public IReactorBuilder Subscribe(Action<object, EventContext> handler) => throw new NotSupportedException();

        public IReactorBuilder Subscribe(Func<object, EventContext, Task> handler) => throw new NotSupportedException();

        public IReactorBuilder OnEventSequence(EventSequenceId eventSequenceId) => throw new NotSupportedException();

        public IReactorBuilder NotReplayable() => throw new NotSupportedException();
    }
}
