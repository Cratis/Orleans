// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_stopping_job_that_doesnt_exist : given.the_manager
{
    JobId _jobId;
    Exception _error;
    Cratis.Monads.Result<StopJobError> _result;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _manager.Stop(_jobId));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_answer_that_it_could_not_stop_it() => (_result.TryGetError(out var refusal) ? refusal : StopJobError.JobIsCompleted).ShouldEqual(StopJobError.Unknown);
    [Fact] void should_get_job_from_storage() => _jobStorage.Received(1).GetJob(_jobId);
}
