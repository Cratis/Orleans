// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_stops_were_counted_but_the_hook_throws : given.a_stopped_job_rejecting_resume
{
    JobStepId _stepId;

    void Establish()
    {
        _stepId = _storedJobStepStates.Single().Id.JobStepId;
        _job.CurrentState.StoppedStepsCounted = [_stepId];
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_restore_the_counted_stops() => _job.CurrentState.StoppedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_save_the_restored_counted_stops() => _job.WrittenStates[^1].StoppedStepsCounted.ShouldContainOnly(_stepId);
}
