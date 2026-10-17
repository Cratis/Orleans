// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_admission_refuses : given.the_job
{
    given.SomeAdmission _admission;
    Mock<given.ISomeJobStep> _step;
    Result<StartJobError> _result;

    void Establish()
    {
        _step = AddJobStep(JobStepId.New());
        _admission = given.SomeAdmission.Refusing();
        Admit(_admission);
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_not_admitted() => ((StartJobError)_result).ShouldEqual(StartJobError.NotAdmitted);
    [Fact] void should_ask_about_the_job_once() => _admission.Asked.Count.ShouldEqual(1);
    [Fact] void should_ask_about_this_job() => _admission.Asked[0].JobId.ShouldEqual(_jobId);
    [Fact] void should_write_nothing() => Writes.ShouldEqual(0);
    [Fact] void should_not_record_any_status() => _job.CurrentState.StatusChanges.Count.ShouldEqual(0);
    [Fact] void should_not_prepare_the_steps_of_the_job() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_prepare_the_step() => _step.Verify(_ => _.Prepare(It.IsAny<object>(), It.IsAny<Cratis.Orleans.Jobs.Stages.JobStepStage>()), Times.Never);
    [Fact] void should_not_start_the_step() => _step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);
}
