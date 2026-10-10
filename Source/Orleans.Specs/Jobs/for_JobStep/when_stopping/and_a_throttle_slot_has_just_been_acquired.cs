// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_throttle_slot_has_just_been_acquired : given.a_job_step_with_a_gated_throttle
{
    PerformWorkResult<JobStepResult> _result;

    async Task Establish()
    {
        var started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        started.IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _throttle.Slot.TrySetResult();
        await _throttle.Acquired.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopped = await _jobStep.Stop(false);
        stopped.IsSuccess.ShouldBeTrue();
        _throttle.ReturnSlot.TrySetResult();
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_fail_with_an_exception() => _result.HasException.ShouldBeFalse();
    [Fact] void should_release_the_acquired_slot() => _throttle.Released.ShouldEqual(1);
}
