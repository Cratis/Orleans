// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using OneOf.Types;

namespace Cratis.Orleans.Jobs.for_JobStepGrainStorageProvider.given;

public class a_job_step_storage_provider : Specification
{
    protected JobStepGrainStorageProvider _provider;
    protected IJobStepStorage _jobStepStorage;
    protected JobId _jobId;
    protected JobStepId _jobStepId;
    protected GrainId _grainId;
    protected IGrainState<JobStepState> _grainState;
    protected JobStepState _state;

    void Establish()
    {
        _jobId = Guid.Parse("a548a45f-94e5-4f6f-bde3-7ff2a807e727");
        _jobStepId = Guid.Parse("b2397c7f-146e-4e18-bb35-ae18ad9c2e38");
        var key = new JobStepKey(_jobId, "some-scope", "some-namespace");
        _grainId = GrainId.Create(GrainType.Create("job-step"), GrainIdKeyExtensions.CreateGuidKey(_jobStepId, key));
        _state = new JobStepState
        {
            Id = new(_jobId, _jobStepId),
            StatusChanges = [new() { Status = JobStepStatus.Scheduled }]
        };
        _grainState = Substitute.For<IGrainState<JobStepState>>();
        _grainState.State.Returns(_state);

        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _jobStepStorage.Remove(Arg.Any<JobId>(), Arg.Any<JobStepId>()).Returns(Task.FromResult(Monads.Catch.Success()));
        _jobStepStorage.Save(Arg.Any<JobId>(), Arg.Any<JobStepId>(), Arg.Any<JobStepState>())
            .Returns(Task.FromResult<Catch<None, Storage.Jobs.JobStepError>>(default(None)));
        _jobStepStorage.MoveToFailed(Arg.Any<JobId>(), Arg.Any<JobStepId>(), Arg.Any<JobStepState>())
            .Returns(Task.FromResult<Catch<None, Storage.Jobs.JobStepError>>(default(None)));
        var jobsStorage = Substitute.For<IJobsStorage>();
        jobsStorage.GetFor(key.Scope, key.Namespace).Returns(new JobsStorage(Substitute.For<IJobStorage>(), _jobStepStorage));
        _provider = new(jobsStorage);
    }
}
