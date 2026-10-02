// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public class RehydrationClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisOrleansSerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddCratisOrleansSerializers();
        var lifecycle = new RehydrationLifecycle();
        var jobs = new[]
        {
            new JobState { Id = JobId.New(), Type = new JobType("RemovedJob"), Status = JobStatus.Running, Created = DateTimeOffset.UtcNow },
            new JobState { Id = JobId.New(), Type = new JobType("RemovedJob"), Status = JobStatus.Running, Created = DateTimeOffset.UtcNow.AddSeconds(1) }
        };
        lifecycle.JobStorage.GetJobs(Arg.Any<JobStatus[]>()).Returns(call =>
            Catch.Success<IImmutableList<JobState>>(call.Arg<JobStatus[]>().Contains(JobStatus.Running) ? [.. jobs] : []));
        lifecycle.JobStorage.GetJob(Arg.Any<JobId>()).Returns(async call =>
        {
            lifecycle.FirstReadStarted.TrySetResult();
            await lifecycle.AllowFirstRead.Task;
            return Catch.Success<JobState, Storage.Jobs.JobError>(jobs.Single(job => job.Id == call.Arg<JobId>()));
        });
        var storage = Substitute.For<IJobsStorage>();
        storage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(lifecycle.JobStorage, Substitute.For<IJobStepStorage>()));
        var types = Substitute.For<IJobTypes>();
        types.GetClrTypeFor(Arg.Any<JobType>()).Returns(Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));

        siloBuilder.Services.AddSingleton(lifecycle);
        siloBuilder.Services.AddSingleton(storage);
        siloBuilder.Services.AddSingleton(types);
        siloBuilder.Services.AddSingleton(Options.Create(new JobsOptions { MaxConcurrentRehydration = 1 }));
    }
}
