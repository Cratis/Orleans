// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_removal_and_the_stored_parent_is_missing : given.a_failed_parent_lookup
{
    void Establish() => _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
    async Task Because() => await StopForRemoval();

    [Fact] void should_complete_the_removal() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_only_write_the_removal_that_deletes_the_step() => _storage.Persisted.Select(step => step.Status).ShouldContainOnly(JobStepStatus.Removing);
    [Fact] void should_not_report_a_stop() => VerifyNoReport();
    [Fact] void should_not_report_failure() => _parent.Verify(job => job.OnStepFailed(It.IsAny<JobStepId>(), It.IsAny<JobStepResult>()), Times.Never);
    [Fact] void should_not_report_success() => _parent.Verify(job => job.OnStepSucceeded(It.IsAny<JobStepId>(), It.IsAny<JobStepResult>()), Times.Never);
    [Fact] void should_not_resolve_a_placeholder_parent() => _jobStep.ResolvedJobIds.ShouldBeEmpty();
    [Fact] void should_settle_the_removal_reservation() => _jobStep.CurrentRun!.IsSettled.ShouldBeTrue();
}
