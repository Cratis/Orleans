// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_stopping_job_that_cannot_persist_the_stop : given.the_manager
{
    JobId _jobId;
    Result<StopJobError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        var job = AddJob<INullJobWithSomeRequest>(_jobId);
        job.Setup(_ => _.Stop()).ReturnsAsync(Result.Failed(StopJobError.CouldNotPersistStop));
    }

    async Task Because() => _result = await _manager.Stop(_jobId);

    [Fact] void should_answer_the_reason() => (_result.TryGetError(out var refusal) ? refusal : StopJobError.Unknown).ShouldEqual(StopJobError.CouldNotPersistStop);
}
