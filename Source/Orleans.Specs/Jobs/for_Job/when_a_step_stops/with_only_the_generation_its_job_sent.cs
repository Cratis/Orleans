// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

/// <summary>
/// A step from a release without generations never removes the generation its job sends, so its report inherits
/// it. That generation says nothing about the run that stopped, so the report follows the rules of that release.
/// </summary>
public class with_only_the_generation_its_job_sent : given.a_running_job_with_two_steps
{
    const string SentToStepsKey = "Cratis.Orleans.Jobs.Generation";

    void Establish()
    {
        // The job was stopped and resumed since the step was started.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
    }

    async Task Because()
    {
        RequestContext.Set(SentToStepsKey, 1L);
        try
        {
            await _job.OnStepStopped(_firstId, Stopped);
        }
        finally
        {
            RequestContext.Remove(SentToStepsKey);
        }
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_hand_the_stop_to_the_job() => _job.HandledStepResults.ShouldContainOnly(_firstId);
    [Fact] void should_not_start_the_step_again() => _first.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);
}
