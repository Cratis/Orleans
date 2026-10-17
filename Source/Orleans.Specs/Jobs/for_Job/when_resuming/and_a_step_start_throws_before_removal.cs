// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_step_start_throws_before_removal : given.a_job_with_a_throwing_resume_start
{
    async Task Because()
    {
        _ = await _job.Resume();
        await _job.Remove();
    }

    [Fact] void should_notify_the_uncertain_dispatch_of_removal() => _steps[0].Verify(step => step.OnJobRemoved(), Times.Once);
    [Fact] void should_notify_the_successful_peer_in_the_same_chunk() => _steps[1].Verify(step => step.OnJobRemoved(), Times.Once);
    [Fact] void should_notify_the_successful_peer_in_the_next_chunk() => _steps[2].Verify(step => step.OnJobRemoved(), Times.Once);
}
