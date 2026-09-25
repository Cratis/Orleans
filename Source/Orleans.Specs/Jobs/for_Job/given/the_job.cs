// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans.TestKit;
using Orleans.Utilities;
namespace Cratis.Orleans.Jobs.for_Job.given;

public class the_job : Specification
{
    protected TestKitSilo _silo = new();
    protected SomeJob _job;
    protected JobId _jobId;
    protected JobKey _jobKey;
    protected IJobsStorage _jobsStorage;
    protected IJobStorage _jobStorage;
    protected IJobStepStorage _jobStepStorage;
    protected IJobTypes _jobTypes;

    protected List<JobStepState> _storedJobStepStates;

    protected int StoredJobStepsWith(JobStepStatus status) => _storedJobStepStates.Count(state => state.Status == status);

    protected Mock<ISomeJobStep> AddJobStep(JobStepId jobStepId, JobStepStage? stage = null)
    {
        var key = new JobStepKey(_jobId, _jobKey.Scope, _jobKey.Namespace);
        _job.StepsToPrepare.Add(new(typeof(ISomeJobStep), jobStepId, key, new SomeRequest(), typeof(object)) { Stage = stage ?? JobStepStage.First });

        var probe = _silo.AddProbe<ISomeJobStep>(jobStepId, keyExtension: key);
        probe.Setup(_ => _.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .Returns((object _, JobStepStage preparedStage) =>
            {
                StoredJobStep(jobStepId).Stage = preparedStage;
                return Task.FromResult(Result<PrepareJobStepError>.Success());
            });
        probe.Setup(_ => _.ReportStatusChange(It.IsAny<JobStepStatus>()))
            .Returns((JobStepStatus status) =>
            {
                RecordJobStepStatus(jobStepId, status);
                return Task.FromResult(Result<JobStepError>.Success());
            });
        return probe;
    }

    protected JobStepState StoredJobStep(JobStepId jobStepId)
    {
        var state = _storedJobStepStates.Find(_ => _.Id.JobStepId == jobStepId);
        if (state is null)
        {
            state = new JobStepState { Id = new(_jobId, jobStepId), Type = typeof(ISomeJobStep) };
            _storedJobStepStates.Add(state);
        }
        return state;
    }

    void RecordJobStepStatus(JobStepId jobStepId, JobStepStatus status) => StoredJobStep(jobStepId).Status = status;

    async Task Establish()
    {
        _storedJobStepStates = [];
        _jobsStorage = Substitute.For<IJobsStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _jobTypes = Substitute.For<IJobTypes>();
        _jobsStorage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(_jobStorage, _jobStepStorage));

        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>()).Returns(_ => Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(_storedJobStepStates.ToImmutableList())));
        _jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("SomeJob")));
        _silo.AddService(_jobsStorage);
        _silo.AddService(_jobTypes);
        _silo.AddService(NullLogger<IJob>.Instance);
        _silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        _jobId = Guid.Parse("fefd1ea0-f739-4d68-8817-6c85f722dec4");
        _jobKey = new("event-store", "namespace");
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
    }
}
