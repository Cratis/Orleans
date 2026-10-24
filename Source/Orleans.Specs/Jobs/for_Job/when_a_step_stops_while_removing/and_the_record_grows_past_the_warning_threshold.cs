// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_the_record_grows_past_the_warning_threshold : given.a_job_being_removed_after_a_resume
{
    JobStepId _firstRecorded;

    void Establish()
    {
        _job.CurrentState.CompletedStepsCounted = Enumerable.Range(0, Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold - 1)
            .Select(_ => JobStepId.New()).ToList();
        _firstRecorded = _job.CurrentState.CompletedStepsCounted[0];
    }

    async Task Because()
    {
        await ReportSuccessFromCurrentRun();
        await _job.OnStepSucceeded(_secondStepId, JobStepResult.Succeeded());
    }

    [Fact] void should_keep_recording_after_the_warning() => _job.CurrentState.CompletedStepsCounted.Count.ShouldEqual(Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold + 1);
    [Fact] void should_not_drop_an_earlier_record() => _job.CurrentState.CompletedStepsCounted.ShouldContain(_firstRecorded);
    [Fact] void should_keep_the_latest_record() => _job.CurrentState.CompletedStepsCounted.ShouldContain(_secondStepId);
}
