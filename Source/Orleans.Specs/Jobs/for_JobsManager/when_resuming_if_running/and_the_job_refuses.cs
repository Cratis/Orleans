// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_resuming_if_running;

public class and_the_job_refuses : given.the_manager
{
    JobId _jobId;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        var job = AddJob<INullJobWithSomeRequest>(_jobId);
        job.Setup(_ => _.ResumeIfRunning()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Failed(default(CannotResumeJobError)));
    }

    async Task Because() => _result = await _manager.ResumeIfRunning(_jobId);

    [Fact] void should_report_an_error() => _result.IsSuccess.ShouldBeFalse();
}
