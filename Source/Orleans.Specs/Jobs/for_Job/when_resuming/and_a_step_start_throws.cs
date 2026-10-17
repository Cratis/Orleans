// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_step_start_throws : given.a_job_with_a_throwing_resume_start
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Because()
    {
        _result = await _job.Resume();
        await _job.Stop();
    }

    [Fact] void should_report_only_the_throwing_step_as_failed_to_resume() => ((FailedResumingJobSteps)(ResumeJobError)_result).FailedJobSteps.ShouldContainOnly(_stepIds[0]);
    [Fact] void should_start_the_other_step_in_the_chunk() => _steps[1].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_start_the_step_in_the_next_chunk() => _steps[2].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_notify_the_uncertain_dispatch_to_stop() => _steps[0].Verify(step => step.OnJobStopped(), Times.Once);
    [Fact] void should_subscribe_the_other_step_in_the_chunk() => _steps[1].Verify(step => step.OnJobStopped(), Times.Once);
    [Fact] void should_subscribe_the_step_in_the_next_chunk() => _steps[2].Verify(step => step.OnJobStopped(), Times.Once);
}
