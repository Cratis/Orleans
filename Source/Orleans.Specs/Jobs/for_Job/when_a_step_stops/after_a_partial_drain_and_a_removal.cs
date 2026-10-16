// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class after_a_partial_drain_and_a_removal : given.a_running_job_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    Result<JobError> _result;
    int _stoppedStepsAfterTheDuplicate;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
        (await _job.StopAndDrain(TimeSpan.Zero)).ShouldEqual(JobDrainStatus.TimedOut);

        // A removal accepts every outcome its steps report.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Removing });
        _job.CurrentState.Status = JobStatus.Removing;
    }

    async Task Because()
    {
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            _result = await _job.OnStepStopped(_firstId, Stopped);
            _stoppedStepsAfterTheDuplicate = _job.CurrentState.Progress.StoppedSteps;

            // The step the drain did not account for reports too, which leaves nothing of the job to wait for.
            RequestContext.Set(GenerationKey, 1L);
            await _job.OnStepStopped(_secondId, Stopped);
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_stop_again() => _stoppedStepsAfterTheDuplicate.ShouldEqual(1);
    [Fact] void should_hand_the_outcome_to_the_job_once() => _job.HandledStepResults.Count(_ => _ == _firstId).ShouldEqual(1);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_complete_the_removal_by_clearing_the_job() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
}
