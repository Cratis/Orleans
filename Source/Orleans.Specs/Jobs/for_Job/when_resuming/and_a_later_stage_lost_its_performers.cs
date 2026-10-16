// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_later_stage_lost_its_performers : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Establish()
    {
        await _job.Start(new());
        await Succeed(_first);
        StoredJobStep(_first).Status = JobStepStatus.CompletedSuccessfully;
        StoredJobStep(_secondA).Status = JobStepStatus.Running;
        StoredJobStep(_secondB).Status = JobStepStatus.Scheduled;
        _secondStepA.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        _secondStepB.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume_the_current_stage() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_restart_the_running_step() => _secondStepA.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_restart_the_scheduled_step() => _secondStepB.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_not_restart_the_completed_stage() => ShouldHaveStarted(_firstStep);
    [Fact] void should_not_start_the_later_stage() => ShouldNotHaveStarted(_thirdStep);
}
