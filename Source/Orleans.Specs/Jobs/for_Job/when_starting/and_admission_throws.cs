// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_admission_throws : given.the_job
{
    Mock<given.ISomeJobStep> _step;
    Result<StartJobError> _result;

    void Establish()
    {
        _step = AddJobStep(JobStepId.New());
        Admit(given.SomeAdmission.Throwing());
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_unknown_rather_than_not_admitted() => ((StartJobError)_result).ShouldEqual(StartJobError.Unknown);
    [Fact] void should_write_nothing() => Writes.ShouldEqual(0);
    [Fact] void should_not_prepare_the_steps_of_the_job() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_start_the_step() => _step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);
}
