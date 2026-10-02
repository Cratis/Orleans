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
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

public class an_observed_job_storage : a_job_storage
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected QueryContext _queryContext;
    protected JobState _thirdJob;
    protected JobState[] _initial;
    protected JobState[] _afterUnknown;
    protected JobState[] _afterKnown;
    protected int _initialTotal;
    protected int _afterUnknownTotal;
    protected int _afterKnownTotal;

    readonly WatchLogger _logger = new();
    readonly List<BsonDocument> _documents = [];
    readonly Channel<BsonDocument> _changes = Channel.CreateUnbounded<BsonDocument>();
    readonly Channel<JobState[]> _emissions = Channel.CreateUnbounded<JobState[]>();
    readonly TaskCompletionSource _unknownRejected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _watchDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ArcApplication _application;
    IDisposable _subscription;
    BsonDocument _watchFilter;
    JobState[] _latest = [];

    protected virtual Paging QueryPaging => Paging.NotPaged;
    protected virtual Sorting QuerySorting => Sorting.None;
    protected virtual bool HasCatalog => true;

    void Establish()
    {
        var queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContext = new QueryContext("Jobs", CorrelationId.New(), QueryPaging, QuerySorting);
        queryContextManager.Current.Returns(_queryContext);
        var host = new HostBuilder().ConfigureServices(services => services.AddLogging(logging => logging.AddProvider(_logger)).AddSingleton(queryContextManager)).Build();
        _application = new ArcApplication(host, new ArcOptions());
        _firstJob.Created = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _secondJob.Created = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _thirdJob = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Created = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), Request = new KnownRequest("third") };
        _unknownJob = new JobState { Id = JobId.New(), Type = new JobType("RemovedJob"), Status = JobStatus.Running, Created = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero), Request = new KnownRequest("unknown") }.ToBsonDocument();
        _documents.AddRange([_unknownJob, _firstJob.ToBsonDocument(), _secondJob.ToBsonDocument()]);
        if (HasCatalog)
        {
            ConfigureCollection(_collection);
        }
        else
        {
            var types = Substitute.For<IJobTypes>();
            types.GetClrTypeFor(Arg.Any<JobType>()).Returns(call => JobTypes.GetClrTypeFor(call.Arg<JobType>()));
            _storage = new JobStorage(_database, types);
            ConfigureCollection(Collection<StoredJob>(WellKnownCollectionNames.Jobs));
        }
    }

    void ConfigureCollection<TDocument>(IMongoCollection<TDocument> collection)
    {
        var serializer = BsonSerializer.LookupSerializer<TDocument>();
        BsonDocument RenderFilter(FilterDefinition<TDocument> filter) => filter.Render(new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry));
        collection.Settings.Returns(new MongoCollectionSettings());
        collection.DocumentSerializer.Returns(serializer);
        collection.CountDocumentsAsync(Arg.Any<FilterDefinition<TDocument>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
            _documents.LongCount(document => Matches(RenderFilter(call.Arg<FilterDefinition<TDocument>>()), document)));
        collection.FindAsync(Arg.Any<FilterDefinition<TDocument>>(), Arg.Any<FindOptions<TDocument, TDocument>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var filter = RenderFilter(call.Arg<FilterDefinition<TDocument>>());
            var options = call.Arg<FindOptions<TDocument, TDocument>>();

            // Model the server: filtering precedes sorting and paging, and only then does typed deserialization run.
            var documents = _documents.Where(document => Matches(filter, document));
            if (options.Sort is not null)
            {
                var sort = options.Sort.Render(new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry)).GetElement(0);
                documents = sort.Value.AsInt32 == 1 ? documents.OrderBy(document => document[sort.Name]) : documents.OrderByDescending(document => document[sort.Name]);
            }
            var states = documents.Skip(options.Skip ?? 0).Take(options.Limit ?? int.MaxValue).Select(document => BsonSerializer.Deserialize<TDocument>(document)).ToArray();
            var cursor = Substitute.For<IAsyncCursor<TDocument>>();
            cursor.Current.Returns(states);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
            return cursor;
        });

        var changeCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<TDocument>>>();
        IEnumerable<ChangeStreamDocument<TDocument>> batch = [];
        changeCursor.Current.Returns(_ => batch);
        changeCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            while (true)
            {
                var change = await _changes.Reader.ReadAsync(call.Arg<CancellationToken>());
                if (!Matches(_watchFilter, change))
                {
                    _unknownRejected.TrySetResult();
                    continue;
                }
                batch = [new ChangeStreamDocument<TDocument>(change, serializer)];
                return true;
            }
        });
        changeCursor.When(cursor => cursor.Dispose()).Do(_ => _watchDisposed.TrySetResult());
        collection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var pipeline = call.Arg<PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>>();
            _watchFilter = pipeline.Render(new RenderArgs<ChangeStreamDocument<TDocument>>(new ChangeStreamDocumentSerializer<TDocument>(serializer), BsonSerializer.SerializerRegistry)).Documents.Single()["$match"].AsBsonDocument;
            return changeCursor;
        });
    }

    protected async Task ObserveInitialJobs()
    {
        _subscription = _storage.ObserveJobs(JobStatus.Running).AsT0.Subscribe(
            jobs =>
            {
                _latest = jobs.ToArray();
                _emissions.Writer.TryWrite(_latest);
            },
            error => _emissions.Writer.TryComplete(error),
            () => _emissions.Writer.TryComplete(_logger.Error));
        _initial = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(Deadline);
        _initialTotal = _queryContext.TotalItems;
    }

    protected async Task InsertUnknownJob()
    {
        var document = _unknownJob.DeepClone().AsBsonDocument;
        document["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
        PushInsert(document);
        await _unknownRejected.Task.WaitAsync(Deadline);
        _afterUnknown = _latest;
        _afterUnknownTotal = _queryContext.TotalItems;
    }

    protected async Task InsertRegisteredJob()
    {
        PushInsert(_thirdJob.ToBsonDocument());
        _afterKnown = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(Deadline);
        _afterKnownTotal = _queryContext.TotalItems;
    }

    protected static BsonDocument Render(FilterDefinition<JobState> filter) => filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry));

    static bool Matches(BsonDocument filter, BsonDocument document) => filter.Elements.All(element => element.Name switch
    {
        "$and" => element.Value.AsBsonArray.All(value => Matches(value.AsBsonDocument, document)),
        "$or" => element.Value.AsBsonArray.Any(value => Matches(value.AsBsonDocument, document)),
        _ => MatchesValue(element.Value, element.Name.Split('.').Aggregate((BsonValue)document, (value, field) => value.AsBsonDocument.GetValue(field, BsonNull.Value)))
    });

    static bool MatchesValue(BsonValue filter, BsonValue value) => filter is BsonDocument operators && operators.TryGetValue("$in", out var values)
        ? values.AsBsonArray.Contains(value)
        : filter.Equals(value);

    async Task Destroy()
    {
        _subscription?.Dispose();
        await _watchDisposed.Task.WaitAsync(Deadline);
        await _application.DisposeAsync();
    }

    void PushInsert(BsonDocument document)
    {
        _documents.Add(document);
        _changes.Writer.TryWrite(new BsonDocument
        {
            { "operationType", "insert" },
            { "documentKey", new BsonDocument("_id", document["_id"]) },
            { "fullDocument", document }
        });
    }
}
