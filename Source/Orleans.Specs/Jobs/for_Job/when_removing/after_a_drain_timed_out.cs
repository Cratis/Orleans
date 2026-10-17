// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class after_a_drain_timed_out : given.a_running_job_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    JobStatus _statusAfterTheRemoval;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
        (await _job.StopAndDrain(TimeSpan.Zero)).ShouldEqual(JobDrainStatus.TimedOut);

        // The harness cannot subscribe to probe steps, so the removal's notification of them fails here and is not
        // what this specifies; the removal's own status write is.
        _ = await _job.Remove();
        _statusAfterTheRemoval = _job.CurrentState.Status;
    }

    async Task Because()
    {
        // The step the drain did not account for reports its stop to the removal.
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            await _job.OnStepStopped(_secondId, Stopped);
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_have_persisted_the_removal_over_the_pending_drain() => _statusAfterTheRemoval.ShouldEqual(JobStatus.Removing);
    [Fact] void should_hand_each_outcome_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_firstId, _secondId);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_complete_the_removal_by_clearing_the_job() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
}
