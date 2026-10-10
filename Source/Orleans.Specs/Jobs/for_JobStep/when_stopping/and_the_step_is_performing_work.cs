// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_is_performing_work : given.a_job_step_left_scheduled_by_a_dead_process
{
    PerformWorkResult<JobStepResult> _result;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        var started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        started.IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopped = await _jobStep.Stop(false);
        stopped.IsSuccess.ShouldBeTrue();
        _jobStep.Gate.TrySetResult();
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_token_supplied_to_the_step() => _jobStep.PerformToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_report_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_fail_with_an_exception() => _result.HasException.ShouldBeFalse();

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
