// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.given;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.given;

public class jobs_in_storage : a_sqlite_jobs_database
{
    protected static readonly JobType KnownJobType = new("custom-job-name");
    protected JobStorage _storage;
    protected IJobTypes _jobTypes;
    protected JsonSerializerOptions _serializerOptions;
    protected JobState _firstJob;
    protected JobState _secondJob;
    protected Job _unknownJob;

    void Establish()
    {
        using var context = CreateContext();
        context.Database.Migrate();

        // Deliberately implements only the original interface: custom registries need no catalog to list jobs.
        _jobTypes = Substitute.For<IJobTypes>();
        _jobTypes.GetClrTypeFor(Arg.Any<JobType>()).Returns(call => call.Arg<JobType>() == KnownJobType
            ? Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(IJob))
            : Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));
        _jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>()).Returns(call => call.Arg<JobType>() == KnownJobType
            ? Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(KnownRequest))
            : Result.Failed<Type, IJobTypes.GetRequestClrTypeForError>(IJobTypes.GetRequestClrTypeForError.CouldNotFindType));
        _jobTypes.GetFor(typeof(IJob)).Returns(KnownJobType);
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.Converters.Add(new ConceptAsJsonConverterFactory());
        _storage = new JobStorage(CreateContext, _jobTypes, _serializerOptions);
        _serializerOptions.Converters.Add(new JobStateConverter(_jobTypes));
        _firstJob = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Request = new KnownRequest("first") };
        _secondJob = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Request = new KnownRequest("second") };
        _unknownJob = new Job { Id = Guid.NewGuid(), Type = "RemovedJob", Status = JobStatus.Running, StateJson = "{\"type\":\"RemovedJob\",\"request\":42}" };
    }

    protected async Task WithJobs(params Job[] jobs)
    {
        await using var context = CreateContext();
        context.Jobs.AddRange(jobs);
        await context.SaveChangesAsync();
    }

    protected Job ToEntity(JobState state) => state.ToEntity(_serializerOptions);

    public record KnownRequest(string Message) : IJobRequest;
}
