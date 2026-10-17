// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_stored_parent_is_missing : given.a_failed_parent_lookup
{
    void Establish() => _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
    async Task Because()
    {
        _stopped = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_report_the_lookup_failure() => _stopped.AsT1.ShouldEqual(JobStepError.FailedToReportToJob);
    [Fact] void should_preserve_the_step_without_writing_stopped_or_removing() => _storage.Persisted.ShouldBeEmpty();
    [Fact] void should_leave_the_persisted_status_recoverable() => _persistedState.Status.ShouldEqual(JobStepStatus.Scheduled);
    [Fact] void should_release_the_stop_only_reservation() => _jobStep.CurrentRun!.IsSettled.ShouldBeTrue();
    [Fact] void should_not_report_to_a_placeholder_parent() => VerifyNoReport();
}
