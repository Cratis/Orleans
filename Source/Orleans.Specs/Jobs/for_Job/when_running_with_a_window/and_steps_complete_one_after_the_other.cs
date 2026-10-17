// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_steps_complete_one_after_the_other : given.a_window_of_two_over_five_steps
{
    int _largestWindow;

    async Task Because()
    {
        _largestWindow = Started;
        for (var index = 0; index < 5; index++)
        {
            await Succeed(index);
            _largestWindow = Math.Max(_largestWindow, Started - (index + 1));
        }
    }

    [Fact] void should_never_have_more_than_the_window_active() => _largestWindow.ShouldEqual(2);
    [Fact] void should_start_every_step_in_the_end() => Started.ShouldEqual(5);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
}
