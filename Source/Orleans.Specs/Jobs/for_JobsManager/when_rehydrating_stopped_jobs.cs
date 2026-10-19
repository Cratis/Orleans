// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_stopped_jobs : given.the_manager
{
    Mock<INullJobWithSomeRequest> _stoppedJob;
    Mock<INullJobWithSomeRequest> _removingJob;
    Mock<INullJobWithSomeRequest> _finalizedJob;

    void Establish()
    {
        _stoppedJob = AddJob<INullJobWithSomeRequest>(Guid.Parse("0b6e1d8a-0f8f-4a50-9b8e-1f0a3a1f0001"));
        _removingJob = AddJob<INullJobWithSomeRequest>(Guid.Parse("0b6e1d8a-0f8f-4a50-9b8e-1f0a3a1f0002"));
        _storedJobs[0].Status = JobStatus.Stopped;
        _storedJobs[1].Status = JobStatus.Removing;
        _finalizedJob = AddJob<INullJobWithSomeRequest>(Guid.Parse("0b6e1d8a-0f8f-4a50-9b8e-1f0a3a1f0003"));
        _storedJobs[2].Status = JobStatus.Stopped;
        _storedJobs[2].StopFinalized = true;
        _stoppedJob.Setup(_ => _.FinalizeStop()).Returns(Task.CompletedTask);
        _removingJob.Setup(_ => _.FinalizeStop()).Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        await _manager.Rehydrate();
        await WaitForRehydration();
    }

    [Fact] void should_finalize_the_stopped_job() => _stoppedJob.Verify(_ => _.FinalizeStop(), Times.Once);
    [Fact] void should_finalize_the_job_being_removed() => _removingJob.Verify(_ => _.FinalizeStop(), Times.Once);
    [Fact] void should_not_resume_the_stopped_job() => _stoppedJob.Verify(_ => _.Resume(), Times.Never);
    [Fact] void should_leave_the_finalized_stopped_job_alone() => _finalizedJob.Invocations.ShouldBeEmpty();
    [Fact] void should_not_resume_the_job_being_removed() => _removingJob.Verify(_ => _.Resume(), Times.Never);
}
