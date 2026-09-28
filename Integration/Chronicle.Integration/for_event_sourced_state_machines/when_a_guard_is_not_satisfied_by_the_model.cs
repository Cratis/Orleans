// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.Integration.Documents;
using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.for_event_sourced_state_machines;

[Collection(StateMachineClusterCollection.Name)]
public class when_a_guard_is_not_satisfied_by_the_model : Specification
{
    const string DocumentId = "document-2";

    StateMachineClusterFixture _fixture = null!;
    string _state = string.Empty;

    void Establish()
    {
        _fixture = StateMachineClusterFixture.Shared;
        InMemoryReadModels.Instance.Clear();
    }

    async Task Because()
    {
        // The model does not say it is submitted, so the guard on the transition keeps it a draft.
        InMemoryReadModels.Instance.Set("tenant-a", DocumentId, new Document("The plan", Submitted: false));
        await _fixture.Registrations.Deliver(new DocumentSubmitted(), EventContexts.For<DocumentSubmitted>("tenant-a", DocumentId, 3));
        _state = await _fixture.GrainFactory.GetEventSourcedStateMachine<IDocumentLifecycle>(EventContexts.EventStore, DocumentId, "tenant-a").GetStateName();
    }

    [Fact] void should_stay_in_its_state() => _state.ShouldEqual(nameof(Draft));
}
