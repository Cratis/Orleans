// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Orleans.Chronicle.StateMachines;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.Documents;

public class DocumentLifecycle : EventSourcedStateMachine<Document>, IDocumentLifecycle
{
    int _modelChanges;

    protected override Type InitialState => typeof(Draft);

    public override IImmutableList<IState<Document>> CreateStates() => [new Draft(), new Submitted()];

    public async Task<string> GetStateName() => (await GetCurrentState()).GetType().Name;

    public Task<int> GetModelChanges() => Task.FromResult(_modelChanges);

    public Task Deactivate()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    protected override void DefineProjection(IProjectionBuilderFor<Document> projection) => projection
        .Passive()
        .From<DocumentDrafted>()
        .From<DocumentSubmitted>(_ => _.Set(document => document.Submitted).ToValue(true))
        .From<DocumentWithdrawn>(_ => _.Set(document => document.Submitted).ToValue(false));

    protected override void DefineTransitions(IStateMachineTransitions<Document> transitions) => transitions
        .On<DocumentSubmitted>().When(document => document.Submitted).TransitionTo<Submitted>()
        .On<DocumentWithdrawn>().TransitionTo<Draft>();

    protected override Type ResolveState(Document? model) => model is { Submitted: true } ? typeof(Submitted) : InitialState;

    protected override Task OnModelChanged(Document? previous, Document? current, EventContext context)
    {
        _modelChanges++;
        return Task.CompletedTask;
    }
}
