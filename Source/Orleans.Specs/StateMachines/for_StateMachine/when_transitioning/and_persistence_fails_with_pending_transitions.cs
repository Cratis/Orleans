// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning;

public class and_persistence_fails_with_pending_transitions : given.a_state_machine_with_a_failing_drain
{
    async Task Because()
    {
        await Admit<StateThatSupportsTransitioningFrom>();
        await Admit<StateThatSupportsTransitioningFrom>();
        _storage.ThrowOnWrite = true;
        await FinishDrain();
        _storage.ThrowOnWrite = false;
        await Admit<StateThatDoesNotChangeStateOnSelfTransition>();
    }

    [Fact] void should_report_the_persistence_failure_to_the_drain_owner() => _failure.ShouldBeOfExactType<SimulatedStateTransitionError>();
    [Fact] void should_discard_all_pending_targets_instead_of_replaying_them_after_a_fresh_request() => _staleEntries.ShouldEqual(0);
    [Fact] void should_persist_the_fresh_transition() => _stateMachine.StoredState.CurrentState.ShouldEqual(typeof(StateThatDoesNotChangeStateOnSelfTransition).FullName);
}
