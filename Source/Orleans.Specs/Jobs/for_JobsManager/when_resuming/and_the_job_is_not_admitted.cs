// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming;

/// <summary>
/// The owner's admission refused the execution. The job stays stopped, so the caller is told it was not taken forward.
/// </summary>
public class and_the_job_is_not_admitted : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    bool _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1025");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Failed(CannotResumeJobError.NotAdmitted));
    }

    async Task Because() => _result = await _manager.Resume(_jobId);

    [Fact] void should_attempt_to_resume_the_job_once() => _job.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_not_report_the_job_as_taken_forward() => _result.ShouldBeFalse();
}
