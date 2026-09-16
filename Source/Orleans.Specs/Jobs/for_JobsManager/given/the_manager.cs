// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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

    protected List<JobState> _storedJobs;

    async Task Establish()
    {
        _storedJobs = [];
        _jobsStorage = Substitute.For<IJobsStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _jobTypes = Substitute.For<IJobTypes>();
        _jobsStorage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(_jobStorage, _jobStepStorage));
        _jobTypes.GetClrTypeFor(Arg.Any<JobType>()).Returns(Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));

        _jobStorage.GetJobs(Arg.Any<JobStatus[]>()).Returns(_ => Task.FromResult(Catch.Success<IImmutableList<JobState>>([.. _storedJobs])));
        _jobStorage.GetJob(Arg.Any<JobId>()).Returns(callInfo => Task.FromResult(
            _storedJobs.SingleOrDefault(job => job.Id == callInfo.Arg<JobId>()) ?? Catch.Failed<JobState, Storage.Jobs.JobError>(Storage.Jobs.JobError.NotFound)));
        _jobStorage.Remove(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));

        _jobStepStorage.RemoveAllForJob(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));

        var options = Options.Create(new JobsOptions());
        _silo.AddService(_jobsStorage);
        _silo.AddService(NullLogger<JobsManager>.Instance);
        _silo.AddService(_jobTypes);
        _silo.AddService(options);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        _managerKey = new("event-store", "namespace");
        _manager = await _silo.CreateGrainAsync<JobsManager>(0, _managerKey);
    }

    protected Mock<TJob> AddJob<TJob>(JobId id)
        where TJob : class, IJob
    {
        var state = new JobState
        {
            Type = typeof(TJob),
            Id = id,
            Created = DateTimeOffset.UtcNow
        };
        _storedJobs.Add(state);
        _jobTypes.GetClrTypeFor(state.Type).Returns(Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(TJob)));
        return _silo.AddProbe<TJob>(state.Id, keyExtension: new JobKey(_managerKey.Scope, _managerKey.Namespace));
    }
}
