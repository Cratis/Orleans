// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_worker_does_not_finish_in_time : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<JobStepError> _stopped;

    protected override TimeSpan StopTimeout => TimeSpan.FromMilliseconds(100);

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _stopped = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // The work settles after the stop gave up on it; its late outcome must go nowhere.
        _jobStep.Gate.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_answer_that_the_stop_timed_out() => ((JobStepError)_stopped).ShouldEqual(JobStepError.StopTimedOut);
    [Fact] void should_not_record_the_step_as_stopped() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Stopped).ShouldBeFalse();
    [Fact] void should_leave_the_step_running() => _persistedState.Status.ShouldEqual(JobStepStatus.Running);
    [Fact] async Task should_not_report_a_stop_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
