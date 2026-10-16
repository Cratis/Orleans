// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_Jobs.given;

public class jobs : Specification
{
    protected Jobs _jobs;
    protected IGrainFactory _factory;
    protected IJobsManager _manager;
    protected job_storage _storage;
    protected IJobTypes _types;
    protected JobsManagerKeys _keys;
    protected INullJobWithSomeRequest _job;
    protected readonly JobKey _key = new("studio", "tenant-a");
    protected readonly JobId _id = JobId.New();
    protected readonly JobType _type = new("known-job");
    protected readonly SomeJobRequest _request = new(42);
    ServiceProvider _services;

    void Establish()
    {
        _factory = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IJobsManager>();
        _storage = Substitute.For<job_storage>(Substitute.For<IJobStorage>());
        _types = Substitute.For<IJobTypes>();
        _keys = new();
        _job = Substitute.For<INullJobWithSomeRequest, IJobDrain>();
        _factory.GetGrain<INullJobWithSomeRequest>(_id, _key).Returns(_job);
        _factory.GetGrain(typeof(INullJobWithSomeRequest), _id, _key).Returns(_job);
        _factory.GetGrain<IJobsManager>(0, new JobsManagerKey(_key.Scope, _key.Namespace)).Returns(_manager);
        _manager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>([]));
        _storage.GetJobType(_id).Returns(Task.FromResult<Cratis.Monads.Catch<JobType, Storage.Jobs.JobError>>(_type));
        _types.GetClrTypeFor(_type).Returns(Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(INullJobWithSomeRequest)));
        _job.Start(_request).Returns(Task.FromResult(Result.Success<StartJobError>()));
        _job.Stop().Returns(Task.FromResult(Result.Success<StopJobError>()));
        _job.Remove().Returns(Task.FromResult(Result.Success<RemoveJobError>()));
        var storage = Substitute.For<IJobsStorage>();
        storage.GetFor(_key.Scope, _key.Namespace).Returns(new JobsStorage(_storage, Substitute.For<IJobStepStorage>()));
        _services = new ServiceCollection().AddSingleton(_keys).BuildServiceProvider();
        _jobs = new(_factory, storage, _types, NullLogger<Jobs>.Instance, _services);
    }

    void Destroy() => _services.Dispose();
}
