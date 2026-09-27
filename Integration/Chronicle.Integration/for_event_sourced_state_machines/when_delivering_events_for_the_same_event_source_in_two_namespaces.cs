// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.Integration.Documents;
using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.for_event_sourced_state_machines;

/// <summary>
/// Events flow from the registered reactor, through grain resolution across a two-silo cluster, into the state machine,
/// which reads its model and transitions - and the same event source in two namespaces is two state machines.
/// </summary>
[Collection(StateMachineClusterCollection.Name)]
public class when_delivering_events_for_the_same_event_source_in_two_namespaces : Specification
{
    const string DocumentId = "document-1";

    StateMachineClusterFixture _fixture = null!;
    IDocumentLifecycle _inTenantA = null!;
    IDocumentLifecycle _inTenantB = null!;
    string _stateInTenantA = string.Empty;
    string _stateInTenantB = string.Empty;
    string _stateInTenantAAfterReactivation = string.Empty;
    int _modelChangesInTenantA;

    void Establish()
    {
        _fixture = StateMachineClusterFixture.Shared;
        InMemoryReadModels.Instance.Clear();
        _inTenantA = _fixture.GrainFactory.GetEventSourcedStateMachine<IDocumentLifecycle>(EventContexts.EventStore, DocumentId, "tenant-a");
        _inTenantB = _fixture.GrainFactory.GetEventSourcedStateMachine<IDocumentLifecycle>(EventContexts.EventStore, DocumentId, "tenant-b");
    }

    async Task Because()
    {
        InMemoryReadModels.Instance.Set("tenant-a", DocumentId, new Document("The plan", Submitted: false));
        InMemoryReadModels.Instance.Set("tenant-b", DocumentId, new Document("Another plan", Submitted: false));
        await _fixture.Registrations.Deliver(new DocumentDrafted("The plan"), EventContexts.For<DocumentDrafted>("tenant-a", DocumentId, 0));
        await _fixture.Registrations.Deliver(new DocumentDrafted("Another plan"), EventContexts.For<DocumentDrafted>("tenant-b", DocumentId, 1));

        InMemoryReadModels.Instance.Set("tenant-a", DocumentId, new Document("The plan", Submitted: true));
        await _fixture.Registrations.Deliver(new DocumentSubmitted(), EventContexts.For<DocumentSubmitted>("tenant-a", DocumentId, 2));

        _stateInTenantA = await _inTenantA.GetStateName();
        _stateInTenantB = await _inTenantB.GetStateName();
        _modelChangesInTenantA = await _inTenantA.GetModelChanges();

        await _inTenantA.Deactivate();
        _stateInTenantAAfterReactivation = await _inTenantA.GetStateName();
    }

    [Fact] void should_transition_the_state_machine_in_the_namespace_the_event_was_appended_in() => _stateInTenantA.ShouldEqual(nameof(Submitted));
    [Fact] void should_leave_the_state_machine_in_the_other_namespace_alone() => _stateInTenantB.ShouldEqual(nameof(Draft));
    [Fact] void should_tell_about_each_model_change_in_its_own_namespace() => _modelChangesInTenantA.ShouldEqual(2);
    [Fact] void should_derive_the_state_from_the_model_when_activated_again() => _stateInTenantAAfterReactivation.ShouldEqual(nameof(Submitted));
}
