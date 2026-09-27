// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// Holds an <see cref="IStorage{TState}"/> a state machine is given directly, recording how it is used.
/// </summary>
/// <remarks>
/// The storage is exposed through a holder rather than taken as a constructor parameter itself, because the test kit
/// replaces any <see cref="IStorage{TState}"/> constructor parameter with its own storage.
/// </remarks>
public class SuppliedStorageForTesting
{
    public SuppliedStorageForTesting() => Storage = new StorageForTesting(this);

    public StateMachineStateForTesting StateToRead { get; set; } = new();
    public int Reads { get; private set; }
    public int Writes { get; private set; }
    public IStorage<StateMachineStateForTesting> Storage { get; }

    sealed class StorageForTesting(SuppliedStorageForTesting owner) : IStorage<StateMachineStateForTesting>
    {
        public StateMachineStateForTesting State { get; set; } = new();
        public string? Etag => null;
        public bool RecordExists => true;

        public Task ClearStateAsync() => Task.CompletedTask;

        public Task ReadStateAsync()
        {
            owner.Reads++;
            State = owner.StateToRead;
            return Task.CompletedTask;
        }

        public Task WriteStateAsync()
        {
            owner.Writes++;
            return Task.CompletedTask;
        }
    }
}
