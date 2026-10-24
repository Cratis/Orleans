// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_resumed_execution_has_a_retained_failure : given.a_stopped_job_rejecting_resume
{
    JobStepId _failedId;

    void Establish()
    {
        _failedId = JobStepId.New();
        StoredJobStep(_failedId).Status = JobStepStatus.CompletedWithFailure;
        _job.CurrentState.StatusChanges.Insert(_job.CurrentState.StatusChanges.Count - 1, new() { Status = JobStatus.Running });
        _job.CurrentState.StatusChanges.Insert(_job.CurrentState.StatusChanges.Count - 1, new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Insert(_job.CurrentState.StatusChanges.Count - 1, new() { Status = JobStatus.Running });
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Progress.FailedSteps = 1;
        _job.BeforeResuming = null;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_the_reconciled_failure() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_failedId);
    [Fact] void should_save_the_reconciled_record() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_failedId);
}
