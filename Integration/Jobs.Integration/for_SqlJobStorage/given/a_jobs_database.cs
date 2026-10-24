// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Json;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cratis.Orleans.Jobs.Integration.for_SqlJobStorage.given;

public abstract class a_jobs_database : Specification, IDisposable
{
    protected static readonly JobType KnownJobType = new("sql-integration-job");
    protected static readonly JobStatus[] RehydratedStatuses =
    [
        JobStatus.Running, JobStatus.PreparingJob, JobStatus.PreparingSteps,
        JobStatus.StartingSteps, JobStatus.Stopped, JobStatus.Removing
    ];

    protected readonly List<string> _commands = [];
    protected JobStorage _storage = null!;
    JsonSerializerOptions _serializerOptions = null!;
    DbContextOptions<JobsDbContext>? _options;
    bool _created;

    protected abstract DbContextOptionsBuilder<JobsDbContext> Configure(DbContextOptionsBuilder<JobsDbContext> builder, string databaseName);

    void Establish()
    {
        _options = Configure(new(), $"cratis_orleans_specs_{Guid.NewGuid():N}")
            .LogTo(_commands.Add, [RelationalEventId.CommandExecuted]).Options;
        using var context = CreateContext();
        context.Database.Migrate();
        _created = true;
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetClrTypeFor(KnownJobType).Returns(Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(IJob)));
        jobTypes.GetRequestClrTypeFor(KnownJobType).Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(KnownRequest)));
        jobTypes.GetFor(typeof(IJob)).Returns(KnownJobType);
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.Converters.Add(new ConceptAsJsonConverterFactory());
        _serializerOptions.Converters.Add(new JobStateConverter(jobTypes));
        _storage = new JobStorage(CreateContext, jobTypes, _serializerOptions);
    }

    public void Dispose()
    {
        if (!_created)
        {
            return;
        }

        using var context = CreateContext();
        if (context.Database.IsNpgsql())
        {
            NpgsqlConnection.ClearAllPools();
        }

        if (context.Database.IsSqlServer())
        {
            SqlConnection.ClearAllPools();
        }

        context.Database.EnsureDeleted();
    }

    protected JobsDbContext CreateContext() => new(_options!);

    protected async Task WithJobs(params Job[] jobs)
    {
        await using var context = CreateContext();
        context.Jobs.AddRange(jobs);
        await context.SaveChangesAsync();
        _commands.Clear();
    }

    protected Job Stop(bool? finalized)
    {
        var job = ToEntity(JobStatus.Stopped, finalized ?? false);
        if (finalized is null)
        {
            var state = JsonNode.Parse(job.StateJson)!.AsObject();
            state.Remove("stopFinalized");
            job.StateJson = state.ToJsonString();
        }

        return job;
    }

    protected Job Raw(JobStatus status, string stateJson) => new()
    {
        Id = Guid.NewGuid(),
        Type = KnownJobType.Value,
        Status = status,
        Created = DateTimeOffset.UtcNow,
        StateJson = stateJson
    };

    protected Job Running() => ToEntity(JobStatus.Running, false);
    protected Job FinalizedRemoval() => ToEntity(JobStatus.Removing, true);

    Job ToEntity(JobStatus status, bool finalized) => new JobState
    {
        Id = JobId.New(),
        Type = KnownJobType,
        Status = status,
        StopFinalized = finalized,
        Created = DateTimeOffset.UtcNow,
        Request = new KnownRequest("storage integration")
    }.ToEntity(_serializerOptions);

    public record KnownRequest(string Message) : IJobRequest;
}
