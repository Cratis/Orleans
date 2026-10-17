// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_earlier_drained_success_arrives_after_the_hook_failed : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    HashSet<JobStepId> _accounted;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
        (await _job.StopAndDrain(TimeSpan.FromSeconds(5))).ShouldEqual(JobDrainStatus.Drained);
        _job.ShouldBeResumable = true;
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
    }

    async Task Because()
    {
        _result = await _job.Resume();
        _accounted = (HashSet<JobStepId>)typeof(Job<given.SomeRequest, given.SomeJobState>)
            .GetField("_accountedByDrain", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!;
        RequestContext.Set("Cratis.Orleans.Jobs.ReportedGeneration", 1L);
        try
        {
            await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove("Cratis.Orleans.Jobs.ReportedGeneration");
        }
    }

    [Fact] void should_report_the_resume_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_drain_accounting_until_hook_success() => _accounted.ShouldContain(_firstId);
    [Fact] void should_count_the_late_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_deliver_the_success_once() => _job.HandledStepResults.Count(id => id == _firstId).ShouldEqual(1);
    [Fact] void should_preserve_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_add_another_stop_callback() => _job.StoppedCount.ShouldEqual(1);
}
