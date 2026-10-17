// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning;

public class and_a_deferred_enter_fails : given.a_state_machine_with_a_failing_drain
{
    async Task Because()
    {
        await Admit<StateThrowingOnEnter>();
        await Admit<StateThatSupportsTransitioningFrom>();
        await Admit<StateThatSupportsTransitioningFrom>();
        await FinishDrain();
        await Admit<StateThatDoesNotChangeStateOnSelfTransition>();
    }

    [Fact] void should_report_the_enter_failure_to_the_drain_owner() => _failure.ShouldBeOfExactType<SimulatedStateTransitionError>();
    [Fact] void should_discard_all_pending_targets_instead_of_replaying_them_after_a_fresh_request() => _staleEntries.ShouldEqual(0);
    [Fact] void should_persist_the_fresh_transition() => _stateMachine.StoredState.CurrentState.ShouldEqual(typeof(StateThatDoesNotChangeStateOnSelfTransition).FullName);
}
