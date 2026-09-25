// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_has_stages : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish() => _job.ShouldBeResumable = true;

    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
        StoredJobStep(_first).Status = JobStepStatus.CompletedSuccessfully;
        _job.CurrentState.Status = JobStatus.Stopped;
        _result = await _job.Resume();
    }

    [Fact] void should_report_the_job_as_resumed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_start_the_first_step_of_the_stage_it_had_reached_again() => _secondStepA.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_start_the_second_step_of_the_stage_it_had_reached_again() => _secondStepB.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_not_start_the_completed_stage_again() => ShouldHaveStarted(_firstStep);
    [Fact] void should_not_start_a_stage_it_had_not_reached() => ShouldNotHaveStarted(_thirdStep);
}
