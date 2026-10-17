// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_stopping_job : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Remove()).ReturnsAsync(Result.Success<RemoveJobError>());
        _job.Setup(_ => _.Stop()).ReturnsAsync(Result.Success<StopJobError>());
    }

    Result<StopJobError> _result;

    async Task Because() => _result = await _manager.Stop(_jobId);

    [Fact] void should_get_job_from_storage() => _jobStorage.Received(1).GetJob(_jobId);
    [Fact] void should_answer_that_the_job_was_stopped() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_stop_the_job() => _job.Verify(_ => _.Stop(), Times.Once);
}
