// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_activating;

public class with_supplied_storage : Specification
{
    const string StoredModel = "Read from supplied storage";

    TestKitSilo _silo;
    SuppliedStorageForTesting _suppliedStorage;
    StateMachineWithSuppliedStorageForTesting _stateMachine;
    StateMachineStateForTesting _onEnterCalledState;

    void Establish()
    {
        _silo = new();
        _suppliedStorage = new()
        {
            StateToRead = new()
            {
                CurrentState = typeof(StateThatSupportsTransitioningFrom).FullName,
                Something = StoredModel
            }
        };
        _silo.AddService(_suppliedStorage);
        _silo.AddService<IEnumerable<IState<StateMachineStateForTesting>>>(
        [
            new StateThatDoesNotSupportTransitioningFrom(),
            new StateThatSupportsTransitioningFrom { OnEnterCalled = _ => _onEnterCalledState = _ }
        ]);
    }

    async Task Because() => _stateMachine = await _silo.CreateGrainAsync<StateMachineWithSuppliedStorageForTesting>(IdSpan.Create(string.Empty));

    [Fact] void should_read_the_stored_state_from_the_supplied_storage_once() => _suppliedStorage.Reads.ShouldEqual(1);
    [Fact] void should_call_on_enter_with_the_stored_state_read() => _onEnterCalledState.Something.ShouldEqual(StoredModel);
    [Fact] async Task should_enter_the_state_recorded_in_the_supplied_storage() => (await _stateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioningFrom>();
    [Fact] void should_write_the_stored_state_to_the_supplied_storage() => _suppliedStorage.Writes.ShouldEqual(1);
    [Fact] void should_keep_the_current_state_in_the_supplied_storage() => _stateMachine.StoredState.CurrentState.ShouldEqual(typeof(StateThatSupportsTransitioningFrom).FullName);
    [Fact] void should_not_use_grain_storage_from_the_runtime() => _silo.StorageManager.GetStorageStats(typeof(StateMachineWithSuppliedStorageForTesting).FullName).ShouldBeNull();
}
