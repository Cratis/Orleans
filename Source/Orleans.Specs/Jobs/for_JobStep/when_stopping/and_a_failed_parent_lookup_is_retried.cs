// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_failed_parent_lookup_is_retried : given.a_job_step_with_a_stored_parent
{
    Result<JobStepError> _first;
    Result<JobStepError> _second;
    bool _preserved;

    async Task Establish()
    {
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
        _first = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _preserved = _storage.Persisted.Count == 0;
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(_parentType));
    }

    async Task Because()
    {
        _second = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_fail_the_first_stop() => _first.AsT1.ShouldEqual(JobStepError.FailedToReportToJob);
    [Fact] void should_preserve_the_step_after_the_failed_lookup() => _preserved.ShouldBeTrue();
    [Fact] void should_accept_the_retry() => _second.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_to_the_parent_once() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] void should_finish_stopping_after_the_retry() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);
}
