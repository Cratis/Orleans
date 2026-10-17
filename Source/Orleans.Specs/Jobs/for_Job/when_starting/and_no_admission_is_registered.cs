// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_no_admission_is_registered : given.the_job
{
    Mock<given.ISomeJobStep> _step;
    Result<StartJobError> _result;

    void Establish()
    {
        _step = AddJobStep(JobStepId.New());
        _step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_start_the_job() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_prepare_the_steps_of_the_job() => _job.PrepareStepsCount.ShouldEqual(1);
    [Fact] void should_start_the_step() => _step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);
}
