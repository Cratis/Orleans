// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_job_is_stopped : given.a_running_job_with_steps
{
    int _callsBefore;

    async Task Establish()
    {
        await _job.Stop();
        CountStepsInStorage();
        _callsBefore = _countingStepStorage.CountCalls;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_look_at_storage() => _countingStepStorage.CountCalls.ShouldEqual(_callsBefore);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
