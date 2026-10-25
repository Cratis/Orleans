// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming_if_running;

public class and_the_job_takes_it : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.ResumeIfRunning()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because() => _result = await _manager.ResumeIfRunning(_jobId);

    [Fact] void should_resume_the_job_conditionally_once() => _job.Verify(_ => _.ResumeIfRunning(), Times.Once);
    [Fact] void should_never_resume_unconditionally() => _job.Verify(_ => _.Resume(), Times.Never);
    [Fact] void should_report_success() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
}
