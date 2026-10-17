// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_two_stops_share_a_pending_parent_lookup : given.a_job_step_with_a_stored_parent
{
    readonly TaskCompletionSource<Catch<JobType, Storage.Jobs.JobError>> _lookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<JobStepError> _first;
    Result<JobStepError> _second;

    void Establish() => _jobStorage.GetJobType(_jobId).Returns(_lookup.Task);

    async Task Because()
    {
        var first = _jobStep.Stop(false);
        var second = _jobStep.Stop(true);
        _lookup.TrySetResult(_parentType);
        _first = await first.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _second = await second.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_accept_the_first_stop() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_removal() => _second.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_resolve_the_parent_once() => await _jobStorage.Received(1).GetJobType(_jobId);
    [Fact] void should_report_the_step_once() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] void should_finish_the_removal() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Removing);

    void Destroy() => _lookup.TrySetResult(_parentType);
}
