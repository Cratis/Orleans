// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming;

public class and_the_job_is_stopped : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    bool _result;

    void Establish()
    {
        _jobId = JobId.New();
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _storedJobs.Single().Status = JobStatus.Stopped;
        _job.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because() => _result = await _manager.Resume(_jobId);

    [Fact] void should_explicitly_resume_the_stopped_job() => _job.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_report_success() => _result.ShouldBeTrue();
}
