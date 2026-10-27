// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_earlier_stop_arrives_after_the_incomplete_stop_retry : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    int _handledBeforeLateStop;
    bool _acknowledged;

    void Establish()
    {
        AllowResume();
        CountStepsInStorage((JobStepStatus.Running, 2));
    }

    async Task Because()
    {
        await _job.Resume();
        _handledBeforeLateStop = _job.HandledStepResults.Count;
        RequestContext.Set(ReportedGenerationKey, _earlierGeneration);
        try
        {
            _acknowledged = (await _job.OnStepStopped(_stepIds[1], Stopped)).IsSuccess;
        }
        finally
        {
            RequestContext.Remove(ReportedGenerationKey);
        }
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_acknowledge_the_older_stop() => _acknowledged.ShouldBeTrue();
    [Fact] void should_count_no_stop_in_this_execution() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_handle_the_older_stop() => _job.HandledStepResults.Count.ShouldEqual(_handledBeforeLateStop);
    [Fact] void should_restart_the_tracked_survivor_after_its_late_stop() => StartsOf(1).ShouldEqual(2);
    [Fact] void should_carry_the_current_generation_on_deferred_restart() => _startsInGeneration.Last(start => start.Step == 1).Generation.ShouldEqual(_resumedGeneration);
    [Fact] void should_not_count_an_already_started_answer_as_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
