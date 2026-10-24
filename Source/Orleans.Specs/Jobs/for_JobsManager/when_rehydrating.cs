// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating : given.the_manager
{
    JobId _firstJobId;
    JobId _secondJobId;
    Mock<INullJobWithSomeRequest> _firstJob;
    Mock<INullJobWithSomeRequest> _secondJob;

    void Establish()
    {
        _firstJobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _secondJobId = Guid.Parse("d090f5e6-5a6a-43b1-8580-4973e3c69521");
        _firstJob = AddJob<INullJobWithSomeRequest>(_firstJobId);
        _secondJob = AddJob<INullJobWithSomeRequest>(_secondJobId);
        _firstJob.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        _secondJob.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because()
    {
        await _manager.Rehydrate();
        await WaitForRehydration();
    }

    [Fact] void should_get_jobs_that_are_interrupted_stopped_or_being_removed() => _jobStorage.Received(1).GetJobsExcludingFinalizedStops(JobStatus.Running, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Stopped, JobStatus.Removing);
    [Fact] void should_not_use_the_unfiltered_read() => _jobStorage.DidNotReceive().GetJobs(Arg.Any<JobStatus[]>());
    [Fact] void should_resume_first_job() => _firstJob.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_resume_second_job() => _secondJob.Verify(_ => _.Resume(), Times.Once);
}
