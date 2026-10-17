// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_prepared_step_has_not_started_after_a_restart : given.a_job_step_with_a_stored_parent
{
    Result<JobStepError> _stopped;

    async Task Because()
    {
        _stopped = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_accept_the_stop() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_stop_to_the_real_parent_exactly_once() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] void should_persist_the_step_as_stopped() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_resolve_the_cold_parent_through_the_virtual_hook() => _jobStep.ResolvedJobIds.ShouldContainOnly(_parentId);
    [Fact] void should_not_launch_work() => _jobStep.WorkRuns.ShouldEqual(0);
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_report_a_failure() => _parent.Verify(job => job.OnStepFailed(It.IsAny<JobStepId>(), It.IsAny<JobStepResult>()), Times.Never);
    [Fact] void should_not_report_success() => _parent.Verify(job => job.OnStepSucceeded(It.IsAny<JobStepId>(), It.IsAny<JobStepResult>()), Times.Never);
}
