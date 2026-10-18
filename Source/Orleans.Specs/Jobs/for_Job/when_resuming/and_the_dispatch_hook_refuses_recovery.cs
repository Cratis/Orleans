// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_dispatch_hook_refuses_recovery : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _job.BeforeDispatching = _ => Task.FromException(new InvalidOperationException("Initialization failed"));
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_startup_recoverable() => _job.CurrentState.Status.ShouldEqual(JobStatus.StartingSteps);
    [Fact] void should_not_dispatch() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_replay_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
}
