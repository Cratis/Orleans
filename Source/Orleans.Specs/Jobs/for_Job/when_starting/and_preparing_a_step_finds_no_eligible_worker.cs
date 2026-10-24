// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Placement;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_preparing_a_step_finds_no_eligible_worker : given.the_job
{
    const string Reason = "no silo has the worker role";

    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<StartJobError> _result;

    void Establish()
    {
        _jobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000005");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<Stages.JobStepStage>()))
            .ThrowsAsync(new NoEligibleJobWorker(GrainId.Create("some-job-step", "some-key"), Reason));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_fail_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_report_an_unknown_error() => ((StartJobError)_result).ShouldEqual(StartJobError.Unknown);
    [Fact] void should_record_the_reason_on_the_status_change() => _job.CurrentState.StatusChanges[^1].ExceptionMessages.Any(message => message.Contains(Reason)).ShouldBeTrue();
    [Fact] void should_try_to_prepare_the_step_once() => _jobStep.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<Stages.JobStepStage>()), Times.Once);
    [Fact] void should_not_start_the_step() => _jobStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
