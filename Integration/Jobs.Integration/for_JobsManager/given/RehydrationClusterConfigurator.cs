// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneOf.Types;
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
        var jobs = Enumerable.Range(0, 7)
            .Select(index => new JobState { Id = JobId.New(), Type = new JobType("RemovedJob"), Status = JobStatus.Running, Created = DateTimeOffset.UtcNow.AddSeconds(index) })
            .ToArray();
        async Task<Cratis.Monads.Catch<IImmutableList<JobState>>> ReadJobs(NSubstitute.Core.CallInfo call)
        {
            if (!call.Arg<JobStatus[]>().Contains(JobStatus.Running))
            {
                return Catch.Success<IImmutableList<JobState>>([]);
            }
            lifecycle.DiscoveryStarted.TrySetResult();
            if (lifecycle.PauseDiscovery)
            {
                await lifecycle.AllowDiscovery.Task;
            }
            return Catch.Success<IImmutableList<JobState>>([.. jobs]);
        }

        lifecycle.JobStorage.GetJobs(Arg.Any<JobStatus[]>()).Returns(ReadJobs);
        lifecycle.JobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(Catch.Success<IImmutableList<JobState>>([])));
        lifecycle.JobStorage.GetJob(Arg.Any<JobId>()).Returns(async call =>
        {
            lifecycle.FirstReadStarted.TrySetResult();
            await lifecycle.AllowFirstRead.Task;
            return Catch.Success<JobState, Storage.Jobs.JobError>(jobs.Single(job => job.Id == call.Arg<JobId>()));
        });
        var storage = Substitute.For<IJobsStorage>();
        storage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(new RehydrationJobStorage(lifecycle.JobStorage), Substitute.For<IJobStepStorage>()));
        var types = Substitute.For<IJobTypes>();
        types.GetClrTypeFor(Arg.Any<JobType>()).Returns(Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));

        siloBuilder.Services.AddSingleton(lifecycle);
        siloBuilder.Services.AddSingleton(storage);
        siloBuilder.Services.AddSingleton(types);
        siloBuilder.Services.AddSingleton(Options.Create(new JobsOptions { MaxConcurrentRehydration = 1 }));
    }

    /// <summary>
    /// Calls the default interface method on a real instance: Castle cannot proxy its implementation without a target.
    /// </summary>
    /// <param name="inner">The substitute that coordinates discovery and job reads.</param>
    sealed class RehydrationJobStorage(IJobStorage inner) : IJobStorage
    {
        public Task<Cratis.Monads.Catch<JobState, Storage.Jobs.JobError>> GetJob(JobId jobId) => inner.GetJob(jobId);
        public Task<Cratis.Monads.Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses) => inner.GetJobs(statuses);
        public Task<Cratis.Monads.Catch<IImmutableList<JobState>>> GetJobs(JobQuery query) => inner.GetJobs(query);
        public Cratis.Monads.Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses) => inner.ObserveJobs(statuses);
        public Task<Cratis.Monads.Catch> Remove(JobId jobId) => inner.Remove(jobId);
        public Task<Cratis.Monads.Catch<bool>> RemoveTerminal(JobId jobId) => inner.RemoveTerminal(jobId);
        public Task<Cratis.Monads.Catch<TJobState, Storage.Jobs.JobError>> Read<TJobState>(JobId jobId) => inner.Read<TJobState>(jobId);
        public Task<Cratis.Monads.Catch<None, Storage.Jobs.JobError>> Save<TJobState>(JobId jobId, TJobState state) => inner.Save(jobId, state);
        public Task<Cratis.Monads.Catch<IImmutableList<TJobState>, Storage.Jobs.JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses) => inner.GetJobs<TJobType, TJobState>(statuses);
    }
}
