// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

public class a_job_storage : Jobs.given.a_database
{
    protected static readonly JobType KnownJobType = new("custom-job-name");
    protected static readonly IJobTypes JobTypes = Substitute.For<IJobTypes, IJobTypesCatalog>();
    protected JobStorage _storage;
    protected IMongoCollection<JobState> _collection;
    protected JobState _firstJob;
    protected JobState _secondJob;
    protected BsonDocument _unknownJob;

    static a_job_storage()
    {
        JobTypes.GetClrTypeFor(Arg.Any<JobType>()).Returns(call => call.Arg<JobType>() == KnownJobType
            ? Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(IJob))
            : Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));
        JobTypes.GetRequestClrTypeFor(Arg.Any<JobType>()).Returns(call => call.Arg<JobType>() == KnownJobType
            ? Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(KnownRequest))
            : Result.Failed<Type, IJobTypes.GetRequestClrTypeForError>(IJobTypes.GetRequestClrTypeForError.CouldNotFindType));
        JobTypes.GetFor(typeof(IJob)).Returns(KnownJobType);
        ((IJobTypesCatalog)JobTypes).All.Returns([KnownJobType]);
        BsonSerializer.RegisterSerializationProvider(new JobStateSerializationProvider(new JobStateSerializer(JobTypes)));
    }

    void Establish()
    {
        _collection = Collection<JobState>(WellKnownCollectionNames.Jobs);
        _storage = new JobStorage(_database, JobTypes);
        _firstJob = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Request = new KnownRequest("first") };
        _secondJob = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Request = new KnownRequest("second") };
        _unknownJob = _firstJob.ToBsonDocument();
        _unknownJob["type"] = "RemovedJob";
    }

    protected void WithDocuments(params BsonDocument[][] batches) => _collection.FindAsync(
        Arg.Any<FilterDefinition<JobState>>(),
        Arg.Any<FindOptions<JobState, BsonDocument>>(),
        Arg.Any<CancellationToken>()).Returns(_ => Cursor(batches));

    public record KnownRequest(string Message) : IJobRequest;
}
