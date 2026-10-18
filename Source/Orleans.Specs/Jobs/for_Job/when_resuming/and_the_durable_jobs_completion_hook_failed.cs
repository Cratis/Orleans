// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_durable_jobs_completion_hook_failed : given.a_job_ready_to_complete
{
    async Task Establish()
    {
        _job.OnCompletedThrows = true;
        await _job.Resume();
        await ActivateFromDurableState();
    }

    Task Because() => _job.Resume();

    [Fact] void should_preserve_the_failed_status() => _job.CurrentState.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_repeat_the_completion_hook_after_reactivation() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_plan_steps_again() => _job.PrepareStepsCount.ShouldEqual(0);
}
