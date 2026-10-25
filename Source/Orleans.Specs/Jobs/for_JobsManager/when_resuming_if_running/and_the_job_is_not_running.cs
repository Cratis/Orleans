// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming_if_running;

public class and_the_job_is_not_running : given.the_manager
{
    JobId _jobId;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        var job = AddJob<INullJobWithSomeRequest>(_jobId);
        job.Setup(_ => _.ResumeIfRunning()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.JobIsNotRunning));
    }

    async Task Because() => _result = await _manager.ResumeIfRunning(_jobId);

    [Fact] void should_report_a_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_that_the_job_is_not_running() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsNotRunning);
}
