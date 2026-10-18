// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_last_live_step_deleted_its_state_before_reporting : given.the_job
{
    JobStepId _stepId;
    Result<ResumeJobSuccess, ResumeJobError> _resume;
    Result<JobError> _report;
    int _completionsBeforeReport;
    int _writes;
    int _successfulSteps;
    int _totalSteps;

    async Task Establish()
    {
        _stepId = JobStepId.New();
        AddJobStep(_stepId).Setup(step => step.Start(It.IsAny<GrainId>()))
            .ReturnsAsync(StartJobStepError.AlreadyStarted);
        await _job.Start(new());
        _job.ShouldBeRemovedAfterCompleted = true;

        // Success deletes the step document before the parent receives OnStepSucceeded.
        _storedJobStepStates.Clear();
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because()
    {
        _resume = await _job.Resume();
        _completionsBeforeReport = _job.AllStepsCompletedCount;
        _report = await _job.OnStepSucceeded(_stepId, JobStepResult.Succeeded());
        _writes = _silo.StorageStats<given.SomeJob, given.SomeJobState>().Writes;
        _successfulSteps = _job.SuccessfulStepsAtCompletion;
        _totalSteps = _job.TotalStepsAtCompletion;
    }

    [Fact] void should_wait_for_the_outcome_before_completing() => _completionsBeforeReport.ShouldEqual(0);
    [Fact] void should_not_claim_completion_from_storage_absence() => ((ResumeJobSuccess)_resume).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_accept_the_pending_outcome() => _report.IsSuccess.ShouldBeTrue();
    [Fact] void should_handle_the_result_before_completing() => _job.ResultsSeenAtCompletion.ShouldEqual(1);
    [Fact] void should_complete_exactly_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_count_one_success() => _successfulSteps.ShouldEqual(1);
    [Fact] void should_preserve_the_planned_step_count() => _totalSteps.ShouldEqual(1);
    [Fact] void should_write_the_resume_and_terminal_statuses() => _writes.ShouldEqual(2);
    [Fact] void should_clear_the_job_once() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
    [Fact] void should_not_write_a_stray_document_after_clearing() => _silo.StorageManager.GetStorage<given.SomeJobState>(typeof(given.SomeJob).FullName!).RecordExists.ShouldBeFalse();
}
