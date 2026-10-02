// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OneOf.Types;

using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IMongoDatabase"/> for persistence.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about <see cref="JobType"/>.</param>
public class JobStorage(IMongoDatabase database, IJobTypes jobTypes) : IJobStorage
{
    const string StatusElementName = "status";
    const string TypeElementName = "type";
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();
    readonly ILogger<JobStorage> _logger = NullLogger<JobStorage>.Instance;
    readonly TransientRetry _retry = TransientRetry.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage"/> class with logging.
    /// </summary>
    /// <param name="database">The database for persistence.</param>
    /// <param name="jobTypes">The registered job types.</param>
    /// <param name="logger">The logger.</param>
    public JobStorage(IMongoDatabase database, IJobTypes jobTypes, ILogger<JobStorage> logger)
        : this(database, jobTypes) => _logger = logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage"/> class with logging and transient error retry.
    /// </summary>
    /// <param name="database">The database for persistence.</param>
    /// <param name="jobTypes">The registered job types.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="retry">The <see cref="TransientRetry"/> applied to storage operations.</param>
    public JobStorage(IMongoDatabase database, IJobTypes jobTypes, ILogger<JobStorage> logger, TransientRetry retry)
        : this(database, jobTypes, logger) => _retry = retry;

    /// <summary>
    /// Gets the indexes maintained on the jobs collection.
    /// </summary>
    /// <remarks>
    /// Keys are declared by stored BSON element name rather than by property expression: <see cref="JobState"/>
    /// is persisted through a custom, non-document serializer that the MongoDB LINQ provider cannot introspect,
    /// so an expression key over an enum member (Status/Type) throws ExpressionNotSupportedException when the
    /// index is rendered against the server's serializer registry.
    /// </remarks>
    internal static IReadOnlyList<CreateIndexModel<JobState>> Indexes { get; } =
    [
        new(
            Builders<JobState>.IndexKeys.Ascending(new StringFieldDefinition<JobState>(StatusElementName)),
            new CreateIndexOptions { Name = "status", Background = true }),
        new(
            Builders<JobState>.IndexKeys
                .Ascending(new StringFieldDefinition<JobState>(TypeElementName))
                .Ascending(new StringFieldDefinition<JobState>(StatusElementName)),
            new CreateIndexOptions { Name = "type_status", Background = true })
    ];

    IMongoCollection<JobState> Collection => database.GetCollection<JobState>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            var job = await _retry.Execute($"getting job {jobId}", async () =>
            {
                using var cursor = await Collection.FindAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);
                return await cursor.SingleOrDefaultAsync();
            }).ConfigureAwait(false);
#pragma warning disable RCS1084 // This is more clear
            return job is not null ? job : JobError.NotFound;
#pragma warning restore RCS1084
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses)
    {
        try
        {
            var jobs = await _retry.Execute("getting jobs", () => GetJobsRaw(statuses)).ConfigureAwait(false);
            return jobs.ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses)
    {
        try
        {
            if (jobTypes is IJobTypesCatalog catalog)
            {
                // Narrow at the server, before the observer applies paging, counts results or tracks membership.
                var registeredTypes = Builders<JobState>.Filter.In(
                    new StringFieldDefinition<JobState, string>(TypeElementName),
                    catalog.All.Select(type => type.Value));
                return Catch.Success(Collection.Observe(registeredTypes & StatusFilter<JobState>(statuses)));
            }

            // Legacy registries cannot supply a server-side type filter. Observe raw requests and skip unknown
            // types before deserializing them. Paging/counts in this fallback still describe the stored rows.
            var source = GetTypedCollection<StoredJob>().Observe(StatusFilter<StoredJob>(statuses));
            return Catch.Success(Subject.Create<IEnumerable<JobState>>(
                Observer.Create<IEnumerable<JobState>>(
                    jobs => source.OnNext(jobs.Select(job => BsonSerializer.Deserialize<StoredJob>(job.ToBsonDocument()))),
                    source.OnError,
                    source.OnCompleted),
                source.Select(documents => (IEnumerable<JobState>)DeserializeJobs<JobState>(documents.Select(document => document.ToBsonDocument())))));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> Remove(JobId jobId)
    {
        try
        {
            await _retry.Execute($"removing job {jobId}", () => Collection.DeleteOneAsync(GetIdFilter<JobState>(jobId))).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }

            var jobState = await _retry.Execute($"reading job {jobId}", async () =>
            {
                using var cursor = await GetTypedCollection<TJobState>().FindAsync(GetIdFilter<TJobState>(jobId)).ConfigureAwait(false);
                return await cursor.FirstOrDefaultAsync();
            }).ConfigureAwait(false);
            return jobState is not null ? jobState : JobError.NotFound;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }

            await _retry.Execute(
                $"saving job {jobId}",
                () => GetTypedCollection<TJobState>().ReplaceOneAsync(GetIdFilter<TJobState>(jobId), state, new ReplaceOptions { IsUpsert = true })).ConfigureAwait(false);

            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }
            if (jobTypes.GetFor(typeof(TJobType)).TryPickT1(out _, out var jobType))
            {
                return JobError.TypeIsNotAssociatedWithAJobType;
            }

            var jobs = await _retry.Execute("getting jobs of type", async () =>
            {
                await EnsureIndexes().ConfigureAwait(false);
                using var cursor = await GetTypedCollection<TJobState>().FindAsync(
                    TypeAndStatusFilter<TJobState>(jobType, statuses),
                    new FindOptions<TJobState, BsonDocument>()).ConfigureAwait(false);
                return await DeserializeJobs<TJobState>(cursor).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return jobs.ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Gets the filter matching jobs in any of the given statuses, or every job when no status is given.
    /// </summary>
    /// <param name="statuses">The <see cref="JobStatus">statuses</see> to match.</param>
    /// <typeparam name="TDocument">The job state document type the filter is built for.</typeparam>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/> to query with.</returns>
    /// <remarks>
    /// Fields are named by stored BSON element name rather than by property expression, for the same reason the
    /// indexes are: <see cref="JobState"/> is persisted through a custom, non-document serializer the MongoDB LINQ
    /// provider cannot introspect, so an expression over an enum member throws ExpressionNotSupportedException when
    /// the filter is rendered against the server's serializer registry.
    /// </remarks>
    internal static FilterDefinition<TDocument> StatusFilter<TDocument>(IReadOnlyCollection<JobStatus> statuses) =>
        statuses.Count == 0
            ? Builders<TDocument>.Filter.Empty
            : Builders<TDocument>.Filter.Or(statuses.Select(status =>
                Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, JobStatus>(StatusElementName), status)));

    /// <summary>
    /// Gets the filter matching jobs of a given type, narrowed to any of the given statuses when statuses are given.
    /// </summary>
    /// <param name="jobType">The <see cref="JobType"/> to match.</param>
    /// <param name="statuses">The <see cref="JobStatus">statuses</see> to narrow to.</param>
    /// <typeparam name="TDocument">The job state document type the filter is built for.</typeparam>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/> to query with.</returns>
    /// <remarks>
    /// Named by stored BSON element name for the reason given on <see cref="StatusFilter{TDocument}"/>.
    /// </remarks>
    internal static FilterDefinition<TDocument> TypeAndStatusFilter<TDocument>(JobType jobType, IReadOnlyCollection<JobStatus> statuses)
    {
        var jobTypeFilter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, JobType>(TypeElementName), jobType);
        return statuses.Count == 0
            ? jobTypeFilter
            : Builders<TDocument>.Filter.And(jobTypeFilter, StatusFilter<TDocument>(statuses));
    }

    static FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid id) => Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, Guid>("_id"), id);

    IMongoCollection<TJobState> GetTypedCollection<TJobState>() => database.GetCollection<TJobState>(WellKnownCollectionNames.Jobs);

    async Task EnsureIndexes() => await Collection.EnsureIndexesOnceAsync(_ensuredIndexes, [.. Indexes]).ConfigureAwait(false);

    async Task<List<JobState>> GetJobsRaw(params JobStatus[] statuses)
    {
        await EnsureIndexes().ConfigureAwait(false);

        // The materialized Status field is kept in lockstep with the last status change (see the Job grain), so
        // filtering it directly is behavior-identical to the previous $arrayElemAt on statusChanges — and indexable.
        using var cursor = await Collection.FindAsync(
            StatusFilter<JobState>(statuses),
            new FindOptions<JobState, BsonDocument>()).ConfigureAwait(false);
        return await DeserializeJobs<JobState>(cursor).ConfigureAwait(false);
    }

    async Task<List<TJobState>> DeserializeJobs<TJobState>(IAsyncCursor<BsonDocument> cursor)
    {
        var jobs = new List<TJobState>();
        while (await cursor.MoveNextAsync().ConfigureAwait(false))
        {
            jobs.AddRange(DeserializeJobs<TJobState>(cursor.Current));
        }
        return jobs;
    }

    List<TJobState> DeserializeJobs<TJobState>(IEnumerable<BsonDocument> documents)
    {
        var jobs = new List<TJobState>();
        foreach (var document in documents)
        {
            var jobType = new JobType(document[TypeElementName].AsString);
            if (!jobTypes.GetClrTypeFor(jobType).IsSuccess)
            {
                _logger.SkippingUnknownJobType(document["_id"].ToString()!, jobType);
                continue;
            }

            // Deserialize only registered types, so an obsolete request cannot abort the entire snapshot.
            jobs.Add(BsonSerializer.Deserialize<TJobState>(document));
        }
        return jobs;
    }
}
