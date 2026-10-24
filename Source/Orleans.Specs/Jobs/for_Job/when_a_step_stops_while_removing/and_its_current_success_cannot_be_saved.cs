// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_current_success_cannot_be_saved : given.a_job_being_removed_after_a_resume
{
    Result<JobError> _result;
    int _attemptsBefore;

    async Task Establish()
    {
        _job.FailWrites = true;
        await ReportSuccessFromCurrentRun();
        _attemptsBefore = _job.WriteAttempts;
    }

    async Task Because() => _result = await ReportStopFromEarlierExecution();

    [Fact] void should_not_acknowledge_unsaved_progress() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_persistence_error() => (_result.TryGetError(out var error) ? error : JobError.UnknownError).ShouldEqual(JobError.PersistStateError);
    [Fact] void should_try_saving_again() => _job.WriteAttempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_keep_the_outcome_record_in_memory() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_handle_the_outcome_once() => _job.HandledStepResults.ShouldContainOnly(_stepId);
    [Fact] void should_not_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_finish_removal() => Clears.ShouldEqual(0);
}
