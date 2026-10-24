// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Placement;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_starting_a_prepared_step_finds_no_eligible_worker : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<StartJobError> _result;

    void Establish()
    {
        _jobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000006");
        _jobStep = AddJobStep(_jobStepId);
        var noWorker = new NoEligibleJobWorker(GrainId.Create("some-job-step", "some-key"), "no silo has the worker role");
        _jobStep.Setup(step => step.Start(It.IsAny<GrainId>())).ThrowsAsync(noWorker);

        // The step cannot be reached to record its failure either.
        _jobStep.Setup(step => step.ReportStatusChange(It.IsAny<JobStepStatus>())).ThrowsAsync(noWorker);
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_that_all_steps_failed_starting() => ((StartJobError)_result).ShouldEqual(StartJobError.AllJobStepsFailedStarting);
    [Fact] void should_count_the_step_as_failed_once() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_attempt_to_record_the_step_as_failed() => _jobStep.Verify(step => step.ReportStatusChange(JobStepStatus.Failed), Times.Once);
    [Fact] void should_try_to_start_the_step_once() => _jobStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
