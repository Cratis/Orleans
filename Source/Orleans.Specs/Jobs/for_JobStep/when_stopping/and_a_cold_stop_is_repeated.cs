// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_cold_stop_is_repeated : given.a_job_step_with_a_stored_parent
{
    async Task Establish()
    {
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because() => (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();

    [Fact] void should_deliver_the_stop_only_once() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] async Task should_resolve_the_parent_only_once() => await _jobStorage.Received(1).GetJobType(_jobId);
    [Fact] void should_persist_only_one_stopped_status() => _storage.Persisted.Count(state => state.Status is JobStepStatus.Stopped).ShouldEqual(1);
    [Fact] async Task should_not_read_the_full_job_request() => await _jobStorage.DidNotReceive().GetJob(_jobId);
}
