// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_orphan_removal_and_the_delete_fails : given.a_failed_parent_lookup
{
    Result<JobStepError> _retry;
    Result<StartJobStepError> _start;

    void Establish()
    {
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
        _storage.WriteFailure = new IOException("The orphan delete did not reach storage");
    }

    async Task Because()
    {
        await StopForRemoval();
        _storage.WriteFailure = null;
        _retry = await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _start = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
    }

    [Fact] void should_report_the_failed_delete() => _stopped.AsT1.ShouldEqual(JobStepError.FailedToPersistState);
    [Fact] void should_retire_the_uncertain_activation() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_not_claim_a_same_activation_retry_succeeded() => _retry.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_start_work_using_uncertain_state() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_repeat_a_write_that_the_provider_might_skip() => _storage.WriteAttempts.ShouldEqual(1);
    [Fact] void should_leave_storage_unchanged_after_the_failed_delete() => _storage.Persisted.ShouldBeEmpty();
    [Fact] void should_not_report_to_the_missing_parent() => VerifyNoReport();
}
