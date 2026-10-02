// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading.Channels;
using Cratis.Arc;
using Cratis.Arc.Queries;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

public class with_an_unknown_type : given.a_job_storage
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    readonly Channel<ChangeStreamDocument<ObservedJobState>> _changes = Channel.CreateUnbounded<ChangeStreamDocument<ObservedJobState>>();
    readonly Channel<JobState[]> _emissions = Channel.CreateUnbounded<JobState[]>();
    readonly TaskCompletionSource _watchDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ArcApplication _application;
    IDisposable _subscription;
    IMongoCollection<ObservedJobState> _observedCollection;
    JobState[] _initial;
    JobState[] _afterUnknown;
    JobState[] _afterKnown;

    void Establish()
    {
        var queryContextManager = Substitute.For<IQueryContextManager>();
        queryContextManager.Current.Returns(new QueryContext("Jobs", CorrelationId.New(), Paging.NotPaged, Sorting.None));
        var host = new HostBuilder().ConfigureServices(services => services.AddLogging().AddSingleton(queryContextManager)).Build();
        _application = new ArcApplication(host, new ArcOptions());
        _observedCollection = Collection<ObservedJobState>(WellKnownCollectionNames.Jobs);
        _observedCollection.Settings.Returns(new MongoCollectionSettings());
        _observedCollection.DocumentSerializer.Returns(BsonSerializer.LookupSerializer<ObservedJobState>());
        _observedCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<ObservedJobState>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(2L);

        _unknownJob["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
        var initialCursor = Substitute.For<IAsyncCursor<ObservedJobState>>();
        initialCursor.Current.Returns([BsonSerializer.Deserialize<ObservedJobState>(_unknownJob), BsonSerializer.Deserialize<ObservedJobState>(_firstJob.ToBsonDocument())]);
        initialCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _observedCollection.FindAsync(Arg.Any<FilterDefinition<ObservedJobState>>(), Arg.Any<FindOptions<ObservedJobState, ObservedJobState>>(), Arg.Any<CancellationToken>()).Returns(initialCursor);

        var changeCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<ObservedJobState>>>();
        IEnumerable<ChangeStreamDocument<ObservedJobState>> batch = [];
        changeCursor.Current.Returns(_ => batch);
        changeCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var change = await _changes.Reader.ReadAsync(call.Arg<CancellationToken>());
            batch = [change];
            return true;
        });
        changeCursor.When(cursor => cursor.Dispose()).Do(_ => _watchDisposed.TrySetResult());
        _observedCollection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<ObservedJobState>, ChangeStreamDocument<ObservedJobState>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(changeCursor);
    }

    async Task Because()
    {
        var result = _storage.ObserveJobs(JobStatus.Running);
        _subscription = result.AsT0.Subscribe(
            jobs => _emissions.Writer.TryWrite(jobs.ToArray()),
            error => _emissions.Writer.TryComplete(error));
        _initial = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(_deadline);
        var unknownInsert = _unknownJob.DeepClone().AsBsonDocument;
        unknownInsert["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
        PushInsert(unknownInsert);
        _afterUnknown = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(_deadline);
        PushInsert(_secondJob.ToBsonDocument());
        _afterKnown = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(_deadline);
    }

    [Fact] void should_skip_unknown_jobs_in_the_initial_snapshot() => _initial.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_skip_unknown_jobs_in_change_stream_snapshots() => _afterUnknown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_continue_observing_registered_jobs() => _afterKnown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_deserialize_registered_requests() => _afterKnown.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request);
    [Fact] void should_preserve_the_status_filter() => _observedCollection.Received(1).FindAsync(
        Arg.Is<FilterDefinition<ObservedJobState>>(filter => filter.Render(new RenderArgs<ObservedJobState>(BsonSerializer.LookupSerializer<ObservedJobState>(), BsonSerializer.SerializerRegistry)).Equals(JobStorage.StatusFilter<ObservedJobState>(new[] { JobStatus.Running }).Render(new RenderArgs<ObservedJobState>(BsonSerializer.LookupSerializer<ObservedJobState>(), BsonSerializer.SerializerRegistry)))),
        Arg.Any<FindOptions<ObservedJobState, ObservedJobState>>(),
        Arg.Any<CancellationToken>());

    async Task Destroy()
    {
        _subscription?.Dispose();
        await _watchDisposed.Task.WaitAsync(_deadline);
        await _application.DisposeAsync();
    }

    void PushInsert(BsonDocument document) => _changes.Writer.TryWrite(new ChangeStreamDocument<ObservedJobState>(
        new BsonDocument
        {
            { "operationType", "insert" },
            { "documentKey", new BsonDocument("_id", document["_id"]) },
            { "fullDocument", document }
        },
        BsonSerializer.LookupSerializer<ObservedJobState>()));
}
