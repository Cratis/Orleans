// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Core;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an <see cref="IStorage{TState}"/> whose state is a Chronicle read model: reading it reads the model through
/// <see cref="Cratis.Chronicle.ReadModels.IReadModels"/>, and writing or clearing it does nothing.
/// </summary>
/// <typeparam name="TModel">Type of read model.</typeparam>
/// <remarks>
/// <para>
/// The events are the only source of truth. The model is derived from them by Chronicle, and the state machine's
/// current state is derived from the model when it activates - so there is nothing for this storage to write, and
/// writing anything would be a second, competing truth. That is also why no storage provider has to be configured.
/// </para>
/// <para>
/// The storage is created before the grain has an identity or services, so it is bound to them when the grain
/// activates, before its first read.
/// </para>
/// </remarks>
public class ReadModelStorage<TModel> : IStorage<TModel>
    where TModel : class
{
    IStateMachineEventStores? _eventStores;
    StateMachineKey? _key;

    /// <summary>
    /// Gets or sets the model, which is null while there is no instance of it.
    /// </summary>
    public TModel State { get; set; } = default!;

    /// <inheritdoc/>
    public string? Etag => null;

    /// <inheritdoc/>
    public bool RecordExists => State is not null;

    /// <summary>
    /// Gets the <see cref="StateMachineKey"/> the storage reads the model for.
    /// </summary>
    /// <exception cref="ReadModelStorageIsNotBound">Thrown when the storage has not been bound yet.</exception>
    public StateMachineKey Key => _key ?? throw new ReadModelStorageIsNotBound(typeof(TModel));

    /// <summary>
    /// Bind the storage to the grain it belongs to.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of the grain, providing the <see cref="IStateMachineEventStores"/>.</param>
    /// <param name="key">The <see cref="StateMachineKey"/> of the grain.</param>
    public void Bind(IServiceProvider services, StateMachineKey key) => Bind(services.GetRequiredService<IStateMachineEventStores>(), key);

    /// <summary>
    /// Bind the storage to the model it reads.
    /// </summary>
    /// <param name="eventStores">The <see cref="IStateMachineEventStores"/> to read through.</param>
    /// <param name="key">The <see cref="StateMachineKey"/> of the model.</param>
    public void Bind(IStateMachineEventStores eventStores, StateMachineKey key)
    {
        _eventStores = eventStores;
        _key = key;
    }

    /// <inheritdoc/>
    /// <exception cref="ReadModelStorageIsNotBound">Thrown when the storage has not been bound yet.</exception>
    public async Task ReadStateAsync()
    {
        if (_eventStores is null || _key is null)
        {
            throw new ReadModelStorageIsNotBound(typeof(TModel));
        }

        var eventStore = await _eventStores.Get(_key.EventStore, _key.Namespace);
        State = await eventStore.ReadModels.GetInstanceById<TModel>(_key.EventSourceId);
    }

    /// <inheritdoc/>
    public Task WriteStateAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ClearStateAsync() => Task.CompletedTask;
}
