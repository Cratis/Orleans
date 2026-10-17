// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_parent_lookup_exceeds_the_stop_deadline : given.a_job_step_with_a_stored_parent
{
    readonly TaskCompletionSource<Catch<JobType, Storage.Jobs.JobError>> _lookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<JobStepError> _first;
    Result<JobStepError> _retry;
    bool _settled;
    bool _preserved;

    protected override TimeSpan StopTimeout => TimeSpan.FromMilliseconds(100);

    void Establish() => _jobStorage.GetJobType(_jobId).Returns(_lookup.Task);

    async Task Because()
    {
        _first = await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _settled = _jobStep.CurrentRun!.IsSettled;
        _preserved = _storage.Persisted.Count == 0;
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(_parentType));
        _retry = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _lookup.TrySetResult(Storage.Jobs.JobError.NotFound);
    }

    [Fact] void should_report_the_lookup_failure_within_the_deadline() => _first.AsT1.ShouldEqual(JobStepError.FailedToReportToJob);
    [Fact] void should_release_the_failed_reservation() => _settled.ShouldBeTrue();
    [Fact] void should_preserve_the_step_after_the_timeout() => _preserved.ShouldBeTrue();
    [Fact] void should_allow_a_later_stop() => _retry.IsSuccess.ShouldBeTrue();
    [Fact] void should_deliver_only_the_retried_stop() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);

    [Fact] void should_not_remove_the_step_for_a_late_missing_parent_answer() => _storage.Persisted.Any(step => step.Status is JobStepStatus.Removing).ShouldBeFalse();

    void Destroy() => _lookup.TrySetResult(Storage.Jobs.JobError.NotFound);
}
