// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_incomplete_stops_hook_still_fails_on_retry : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    bool _retryFailed;
    int _startsWhileRejected;
    long _generationWhileRejected;
    JobStatus _statusWhileRejected;

    async Task Because()
    {
        _retryFailed = !(await _job.Resume()).IsSuccess;
        _startsWhileRejected = StartsOf(0);
        _generationWhileRejected = _job.CurrentState.StatusChanges.Count;
        _statusWhileRejected = _job.CurrentState.Status;
        AllowResume();
        await _job.Resume();
    }

    [Fact] void should_reject_the_retry() => _retryFailed.ShouldBeTrue();
    [Fact] void should_not_start_the_stopped_candidate_before_hook_success() => _startsWhileRejected.ShouldEqual(0);
    [Fact] void should_remain_running_after_rejection() => _statusWhileRejected.ShouldEqual(JobStatus.Running);
    [Fact] void should_keep_the_generation_after_rejection() => _generationWhileRejected.ShouldEqual(_resumedGeneration);
    [Fact] void should_recover_the_stopped_candidate_on_a_later_successful_retry() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_not_write_another_transition() => Writes.ShouldEqual(_writesAfterRejectedResume);
}
