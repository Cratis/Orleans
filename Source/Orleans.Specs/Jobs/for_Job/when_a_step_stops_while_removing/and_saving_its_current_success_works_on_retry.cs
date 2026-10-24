// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_saving_its_current_success_works_on_retry : given.a_job_being_removed_after_a_resume
{
    Result<JobError> _result;

    async Task Establish()
    {
        _job.FailWrites = true;
        await ReportSuccessFromCurrentRun();
        _job.FailWrites = false;
    }

    async Task Because() => _result = await ReportStopFromEarlierExecution();

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_save_the_success_once() => _job.WrittenStates[^1].Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_save_the_recorded_outcome() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_not_save_a_stop() => _job.WrittenStates[^1].Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_handle_the_outcome_once() => _job.HandledStepResults.ShouldContainOnly(_stepId);
    [Fact] void should_not_finish_removal() => Clears.ShouldEqual(0);
}
