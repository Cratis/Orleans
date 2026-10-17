// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_the_cold_step_cannot_resolve_a_parent : given.a_job_step_with_a_stored_parent
{
    JobStepDrainResult _result;

    void Establish() => _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
    async Task Because() => _result = await _jobStep.Drain(1, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_still_drain() => _result.Drained.ShouldBeTrue();
    [Fact] void should_return_the_stopped_outcome_for_the_job_to_account() => _result.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] async Task should_not_look_up_the_parent() => await _jobStorage.DidNotReceive().GetJobType(_jobId);
    [Fact] void should_not_send_a_parent_report() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Never);
    [Fact] void should_persist_the_drained_outcome() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);
}
