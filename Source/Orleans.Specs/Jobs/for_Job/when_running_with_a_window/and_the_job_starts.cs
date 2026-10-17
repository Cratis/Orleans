// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_the_job_starts : given.a_window_of_two_over_five_steps
{
    [Fact] void should_start_no_more_steps_than_the_window() => Started.ShouldEqual(2);
    [Fact] void should_start_the_first_step() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_start_the_second_step() => StartsOf(1).ShouldEqual(1);
    [Fact] void should_leave_the_others_waiting() => new[] { StartsOf(2), StartsOf(3), StartsOf(4) }.ShouldEachConformTo(starts => starts == 0);
    [Fact] void should_run_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_count_every_step_as_part_of_the_job() => _job.CurrentState.Progress.TotalSteps.ShouldEqual(5);
}
