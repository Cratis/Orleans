// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_the_record_grows_past_the_warning_threshold : given.a_job_being_removed_after_a_resume
{
    readonly ILogger<Job<given.SomeRequest, given.SomeJobState>> _logger = Substitute.For<ILogger<Job<given.SomeRequest, given.SomeJobState>>>();
    JobStepId _firstRecorded;

    async Task Establish()
    {
        _job.CurrentState.CompletedStepsCounted = Enumerable.Range(0, Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold - 1)
            .Select(_ => JobStepId.New()).ToList();
        _firstRecorded = _job.CurrentState.CompletedStepsCounted[0];
        _job.CurrentState.Progress.TotalSteps = Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold + 2;
        _job.CurrentState.Progress.SuccessfulSteps = Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold - 1;
        StoredJobStep(JobStepId.New()).Status = JobStepStatus.Running;
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        await ReactivateFrom(_job.CurrentState, _logger);
    }

    async Task Because()
    {
        await ReportSuccessFromCurrentRun();
        await _job.OnStepSucceeded(_secondStepId, JobStepResult.Succeeded());
        using var generation = JobExecutionGeneration.Report(EarlierGeneration);
        await _job.OnStepStopped(_firstRecorded, Stopped);
    }

    [Fact] void should_keep_recording_after_the_warning() => _job.CurrentState.CompletedStepsCounted.Count.ShouldEqual(Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold + 1);
    [Fact] void should_not_drop_an_earlier_record() => _job.CurrentState.CompletedStepsCounted.ShouldContain(_firstRecorded);
    [Fact] void should_keep_the_latest_record() => _job.CurrentState.CompletedStepsCounted.ShouldContain(_secondStepId);
    [Fact] void should_save_the_record_past_the_threshold() => _job.WrittenStates[^1].CompletedStepsCounted.Count.ShouldEqual(Job<given.SomeRequest, given.SomeJobState>.CountedOutcomesWarningThreshold + 1);
    [Fact] void should_ignore_a_stop_for_a_preset_record() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_wait_for_the_outstanding_step() => Clears.ShouldEqual(0);
    [Fact]
    void should_warn_only_once() => _logger.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        call.GetArguments()[0] is LogLevel.Warning).ShouldEqual(1);
}
