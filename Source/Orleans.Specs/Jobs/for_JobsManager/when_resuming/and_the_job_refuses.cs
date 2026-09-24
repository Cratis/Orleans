// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming;

/// <summary>
/// A job can decline to resume - it was never prepared, or its owner is no longer interested. The job then stays
/// exactly as it was: it will not run, will not finalize, and will never report back. Reporting that as success
/// leaves the caller waiting for something that is not coming.
/// </summary>
public class and_the_job_refuses : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    bool _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Failed(default(CannotResumeJobError)));
    }

    async Task Because() => _result = await _manager.Resume(_jobId);

    [Fact] void should_attempt_to_resume_the_job() => _job.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_not_report_the_job_as_taken_forward() => _result.ShouldBeFalse();
}
