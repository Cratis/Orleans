// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OneOf.Types;
using Orleans.TestKit;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobsManager.given;

public class the_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected JobsManager _manager;
    protected JobsManagerKey _managerKey;
    protected IJobsStorage _jobsStorage;
    protected IJobStorage _jobStorage;
    protected IJobStepStorage _jobStepStorage;
    protected IJobTypes _jobTypes;
    protected IOptions<JobsOptions> _options;
    protected CancellationTokenSource _applicationStopping;
    protected Task Rehydration => (Task)typeof(JobsManager).GetField("_rehydration", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_manager)!;
    protected bool _deactivated;

    protected List<JobState> _storedJobs;

    async Task Establish()
    {
        _storedJobs = [];
        _jobsStorage = Substitute.For<IJobsStorage>();
        _jobStorage = Substitute.For<job_storage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _jobTypes = Substitute.For<IJobTypes>();
        _jobsStorage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(_jobStorage, _jobStepStorage));
        _jobTypes.GetClrTypeFor(Arg.Any<JobType>()).Returns(Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));

        _jobStorage.GetJobs(Arg.Any<JobStatus[]>()).Returns(_ => Task.FromResult(Catch.Success<IImmutableList<JobState>>([.. _storedJobs])));

        // Deliberately unfiltered: the manager must still drop finalized stops itself, whatever the provider returns.
        _jobStorage.GetJobsExcludingFinalizedStops(Arg.Any<JobStatus[]>()).Returns(_ => Task.FromResult(Catch.Success<IImmutableList<JobState>>([.. _storedJobs])));
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(callInfo =>
        {
            var query = callInfo.Arg<JobQuery>();
            var jobs = _storedJobs.Where(job =>
                (query.Type is null || job.Type == query.Type) &&
                (query.Statuses.Count == 0 || query.Statuses.Contains(job.Status)) &&
                (query.CreatedBefore is null || job.Created < query.CreatedBefore))
                .OrderBy(job => job.Created)
                .ThenBy(job => job.Id)
                .Skip(query.Skip)
                .Take(query.Take)
                .ToImmutableList();
            return Task.FromResult(Catch.Success<IImmutableList<JobState>>(jobs));
        });
        _jobStorage.GetJob(Arg.Any<JobId>()).Returns(callInfo => Task.FromResult(
            _storedJobs.SingleOrDefault(job => job.Id == callInfo.Arg<JobId>()) ?? Catch.Failed<JobState, Storage.Jobs.JobError>(Storage.Jobs.JobError.NotFound)));
        _jobStorage.Remove(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));
        _jobStorage.RemoveTerminal(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success(false)));

        _jobStepStorage.RemoveAllForJob(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));

        _options = Substitute.For<IOptions<JobsOptions>>();
        _options.Value.Returns(CreateOptions());
        _applicationStopping = new();
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Returns(_applicationStopping.Token);
        _silo.AddService(applicationLifetime);
        _silo.AddService(_jobsStorage);
        _silo.AddService(CreateLogger());
        _silo.AddService(_jobTypes);
        _silo.AddService(_options);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        _managerKey = new("event-store", "namespace");
        _manager = await _silo.CreateGrainAsync<JobsManager>(0, _managerKey);
    }

    async Task Destroy()
    {
        if (!_deactivated)
        {
            await _manager.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        }
        _applicationStopping.Dispose();
    }

    /// <summary>
    /// Lets Castle proxy the default interface method without a target instance.
    /// </summary>
    public abstract class job_storage : IJobStorage
    {
        public abstract Task<Catch<JobState, Storage.Jobs.JobError>> GetJob(JobId jobId);
        public abstract Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses);
        public abstract Task<Catch<IImmutableList<JobState>>> GetJobsExcludingFinalizedStops(params JobStatus[] statuses);
        public abstract Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query);
        public abstract Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses);
        public abstract Task<Catch> Remove(JobId jobId);
        public abstract Task<Catch<bool>> RemoveTerminal(JobId jobId);
        public abstract Task<Catch<TJobState, Storage.Jobs.JobError>> Read<TJobState>(JobId jobId);
        public abstract Task<Catch<None, Storage.Jobs.JobError>> Save<TJobState>(JobId jobId, TJobState state);
        public abstract Task<Catch<IImmutableList<TJobState>, Storage.Jobs.JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses);
    }

    protected virtual JobsOptions CreateOptions() => new();

    protected virtual ILogger<JobsManager> CreateLogger() => NullLogger<JobsManager>.Instance;

    protected Task WaitForRehydration() => Rehydration.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    protected Mock<TJob> AddJob<TJob>(JobId id)
        where TJob : class, IJob
    {
        var state = new JobState
        {
            Type = typeof(TJob),
            Id = id,
            Status = JobStatus.Running,
            Created = DateTimeOffset.UtcNow
        };
        _storedJobs.Add(state);
        _jobTypes.GetClrTypeFor(state.Type).Returns(Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(TJob)));
        return _silo.AddProbe<TJob>(state.Id, keyExtension: new JobKey(_managerKey.Scope, _managerKey.Namespace));
    }
}
