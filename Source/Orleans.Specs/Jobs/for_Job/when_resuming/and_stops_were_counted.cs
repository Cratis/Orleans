// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_stops_were_counted : given.a_stopped_job_rejecting_resume
{
    IList<JobStepId> _countedWhenResumed = [];

    void Establish()
    {
        _job.CurrentState.StoppedStepsCounted = [_storedJobStepStates.Single().Id.JobStepId];
        _job.BeforeResuming = () =>
        {
            _countedWhenResumed = _job.WrittenStates[^1].StoppedStepsCounted;
            return Task.CompletedTask;
        };
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_save_the_new_execution_without_counted_stops() => _countedWhenResumed.ShouldBeEmpty();
}
