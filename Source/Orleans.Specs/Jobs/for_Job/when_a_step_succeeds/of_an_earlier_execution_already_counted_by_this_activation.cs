// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

/// <summary>
/// A step's success was counted by this activation and the step is no longer tracked, after which the same success
/// arrives again from an earlier execution. It counts nothing the second time.
/// </summary>
public class of_an_earlier_execution_already_counted_by_this_activation : when_running_in_stages.given.a_job_with_three_stages
{
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    int _successfulAfterTheFirstReport;

    async Task Establish()
    {
        await _job.Start(new());
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.ShouldBeResumable = true;
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
        await Succeed(_first);
        _successfulAfterTheFirstReport = _job.CurrentState.Progress.SuccessfulSteps;
    }

    async Task Because()
    {
        RequestContext.Set(ReportedKey, 1L);
        try
        {
            await _job.OnStepSucceeded(_first, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(ReportedKey);
        }
    }

    [Fact] void should_count_the_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(_successfulAfterTheFirstReport);
}
