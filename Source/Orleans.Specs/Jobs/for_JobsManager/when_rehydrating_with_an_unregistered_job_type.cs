// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_with_an_unregistered_job_type : given.the_manager
{
    Mock<INullJobWithSomeRequest> _firstJob;
    Mock<INullJobWithSomeRequest> _secondJob;

    void Establish()
    {
        _firstJob = AddJob<INullJobWithSomeRequest>(JobId.New());
        _storedJobs.Add(new JobState { Id = JobId.New(), Type = new JobType("RemovedJob"), Status = JobStatus.Running, Created = DateTimeOffset.UtcNow });
        _secondJob = AddJob<INullJobWithSomeRequest>(JobId.New());
        _firstJob.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        _secondJob.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    Task Because() => _manager.Rehydrate();

    [Fact] void should_resume_the_first_registered_job() => _firstJob.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_resume_the_second_registered_job() => _secondJob.Verify(job => job.Resume(), Times.Once);
}
