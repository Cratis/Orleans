// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.given;

public class registrations : Specification
{
    protected IClientArtifactsProvider _artifacts;
    protected List<Type> _discoveredModelBoundProjections;
    protected IStateMachineArtifactRegistrations _registrations;
    protected Dictionary<ReactorId, Action<IReactorBuilder>> _reactors;
    protected List<EventSourcedStateMachineDefinition> _reactorsCreatedFor;

    void Establish()
    {
        _discoveredModelBoundProjections = [];
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.EventTypes.Returns(KnownEventTypes.All);
        _artifacts.Projections.Returns([]);
        _artifacts.ModelBoundProjections.Returns(_ => _discoveredModelBoundProjections);

        _reactors = [];
        _reactorsCreatedFor = [];
        _registrations = Substitute.For<IStateMachineArtifactRegistrations>();
        _registrations
            .When(_ => _.RegisterReactor(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorBuilder>>()))
            .Do(call => _reactors[call.Arg<ReactorId>()] = call.Arg<Action<IReactorBuilder>>());
    }

    protected void Register(params Type[] stateMachineTypes) =>
        EventSourcedStateMachineRegistrar.Register(
            stateMachineTypes.Select(_ => EventSourcedStateMachineInspector.Inspect(_, _artifacts)),
            _registrations,
            definition =>
            {
                _reactorsCreatedFor.Add(definition);
                return new(definition, () => Substitute.For<IStateMachineGrainResolver>(), () => Substitute.For<IStateMachineEventStores>(), new());
            });
}
