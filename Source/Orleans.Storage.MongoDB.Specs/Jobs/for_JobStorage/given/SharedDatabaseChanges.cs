// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Subjects;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

/// <summary>
/// Stands in for the database's shared change stream, the way Arc's watcher serves it to an observation.
/// </summary>
public sealed class SharedDatabaseChanges : IObservable<ChangeStreamDocument<BsonDocument>>
{
    readonly Subject<ChangeStreamDocument<BsonDocument>> _subject = new();
    readonly TaskCompletionSource _unsubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a task that completes when the observation stops listening.
    /// </summary>
    public Task Unsubscribed => _unsubscribed.Task;

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<ChangeStreamDocument<BsonDocument>> observer)
    {
        var subscription = _subject.Subscribe(observer);
        return Disposable.Create(() =>
        {
            subscription.Dispose();
            _unsubscribed.TrySetResult();
        });
    }

    /// <summary>
    /// Delivers a change, as if the server had emitted it.
    /// </summary>
    /// <param name="change">The change to deliver.</param>
    public void Push(ChangeStreamDocument<BsonDocument> change) => _subject.OnNext(change);
}
