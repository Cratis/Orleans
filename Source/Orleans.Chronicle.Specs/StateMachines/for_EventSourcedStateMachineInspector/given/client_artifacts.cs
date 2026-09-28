// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.given;

public class client_artifacts : Specification
{
    protected IClientArtifactsProvider _artifacts;
    protected List<Type> _discoveredProjections;
    protected List<Type> _discoveredModelBoundProjections;

    void Establish()
    {
        _discoveredProjections = [];
        _discoveredModelBoundProjections = [];
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.EventTypes.Returns(KnownEventTypes.All);
        _artifacts.Projections.Returns(_ => _discoveredProjections);
        _artifacts.ModelBoundProjections.Returns(_ => _discoveredModelBoundProjections);
    }
}
