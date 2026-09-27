// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Orleans.Chronicle.Integration.Documents;

namespace Cratis.Orleans.Chronicle.Integration.for_event_sourced_state_machines;

[Collection(StateMachineClusterCollection.Name)]
public class when_registering_with_chronicle : Specification
{
    RecordingRegistrations _registrations = null!;

    void Because() => _registrations = StateMachineClusterFixture.Shared.Registrations;

    [Fact] void should_register_the_projection_the_state_machine_defines() => _registrations.RegisteredProjections.ShouldContainOnly(typeof(Document));
    [Fact] void should_register_one_reactor_for_the_state_machine() => _registrations.RegisteredReactors.ShouldContainOnly(new ReactorId(typeof(DocumentLifecycle).FullName!));
    [Fact] void should_subscribe_to_exactly_the_events_of_the_projection_and_transitions() => _registrations.SubscribedEventTypes.ShouldContainOnly(typeof(DocumentDrafted), typeof(DocumentSubmitted), typeof(DocumentWithdrawn));
}
