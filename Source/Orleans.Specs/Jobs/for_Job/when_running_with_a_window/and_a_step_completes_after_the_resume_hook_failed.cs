// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_a_step_completes_after_the_resume_hook_failed : given.a_window_of_two_over_five_steps
{
    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        _ = await _job.Resume();
    }

    Task Because() => Succeed(0);

    [Fact] void should_not_start_the_next_step() => StartsOf(2).ShouldEqual(0);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
