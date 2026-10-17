// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_the_job_failed_with_live_steps : given.a_running_job_with_two_steps
{
    JobDrainStatus _whileAStepRuns;
    JobDrainStatus _result;

    async Task Establish()
    {
        _job.CurrentState.Status = JobStatus.Failed;
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
        _whileAStepRuns = await _job.StopAndDrain(TimeSpan.Zero);
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.Zero);

    [Fact] void should_time_out_while_a_step_still_runs() => _whileAStepRuns.ShouldEqual(JobDrainStatus.TimedOut);
    [Fact] void should_answer_completed_once_every_step_drained() => _result.ShouldEqual(JobDrainStatus.Completed);
    [Fact] void should_stay_failed() => _job.CurrentState.Status.ShouldEqual(JobStatus.Failed);
}
