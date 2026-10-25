// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_resuming_if_running_a_job_that_doesnt_exist : given.the_manager
{
    JobId _jobId;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish() => _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");

    async Task Because() => _result = await _manager.ResumeIfRunning(_jobId);

    [Fact] void should_get_job_from_storage() => _jobStorage.Received(1).GetJob(_jobId);
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_as_unknown() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.Unknown);
}
