// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.given;

public abstract class a_reactor : Specification
{
    protected EventStoresForTesting _eventStores;
    protected IStateMachineGrainResolver _grains;
    protected Dictionary<string, IEventSourcedStateMachine> _grainsByKey;
    protected EventSourcedStateMachinesOptions _options;
    protected EventSourcedStateMachineDefinition _definition;

    protected abstract Type StateMachineType { get; }

    internal EventSourcedStateMachineReactor Reactor => new(_definition, () => _grains, () => _eventStores.Provider, _options);

    void Establish()
    {
        _eventStores = new();
        _grainsByKey = [];
        _grains = Substitute.For<IStateMachineGrainResolver>();
        _grains.Get(Arg.Any<Type>(), Arg.Any<StateMachineKey>()).Returns(call => GrainFor(call.Arg<StateMachineKey>()));
        _options = new() { ModelCatchUpPollInterval = TimeSpan.Zero, ModelCatchUpTimeout = TimeSpan.FromMinutes(1) };

        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns(KnownEventTypes.All);
        artifacts.Projections.Returns([]);
        artifacts.ModelBoundProjections.Returns([]);
        _definition = EventSourcedStateMachineInspector.Inspect(StateMachineType, artifacts);
    }

    protected IEventSourcedStateMachine GrainFor(StateMachineKey key)
    {
        if (!_grainsByKey.TryGetValue(key.ToString(), out var grain))
        {
            grain = Substitute.For<IEventSourcedStateMachine>();
            _grainsByKey[key.ToString()] = grain;
        }

        return grain;
    }

    protected static ProjectionState ProjectionAt(ulong lastHandled) =>
        new(ObserverRunningState.Active, true, lastHandled + 1, lastHandled, lastHandled);
}
