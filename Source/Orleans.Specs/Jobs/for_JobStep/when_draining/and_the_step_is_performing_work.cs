// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_the_step_is_performing_work : given.a_job_step_left_scheduled_by_a_dead_process
{
    JobStepDrainResult _result;
    bool _drainedBeforeTheWorkSettled;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var draining = _jobStep.Drain(1, TimeSpan.FromSeconds(5));
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _drainedBeforeTheWorkSettled = draining.IsCompleted;
        _jobStep.Gate.TrySetResult();
        _result = await draining.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _jobStep.PerformToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_answer_before_the_work_settled() => _drainedBeforeTheWorkSettled.ShouldBeFalse();
    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_answer_stopped() => _result.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_answer_the_outcome_of_the_run() => _result.Outcome.ShouldNotBeNull();
    [Fact] void should_have_persisted_the_stop_before_answering() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Stopped).ShouldEqual(1);

    async Task Destroy()
    {
        _jobStep.Gate.TrySetResult();
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
