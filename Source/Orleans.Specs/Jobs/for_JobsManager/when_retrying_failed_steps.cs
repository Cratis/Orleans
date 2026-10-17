// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_retrying_failed_steps : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    Result<int, RetryFailedStepsError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.RetryFailed()).ReturnsAsync(Result.Success<int, RetryFailedStepsError>(3));
    }

    async Task Because() => _result = await _manager.RetryFailedSteps(_jobId);

    [Fact] void should_ask_the_job() => _job.Verify(_ => _.RetryFailed(), Times.Once);
    [Fact] void should_answer_how_many_steps_were_run_again() => _result.Match(count => count, _ => -1).ShouldEqual(3);
}
