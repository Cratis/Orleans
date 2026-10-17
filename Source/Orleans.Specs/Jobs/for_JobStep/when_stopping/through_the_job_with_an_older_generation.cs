// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class through_the_job_with_an_older_generation : given.a_job_step_left_scheduled_by_a_dead_process
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    async Task Establish()
    {
        // The step runs in generation 2, after the job was resumed.
        _jobStep.HoldPerformOpen = true;
        RequestContext.Set(GenerationKey, 2L);
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        RequestContext.Remove(GenerationKey);
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        // The notification of the stop that the resume overtook arrives late.
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            await _jobStep.OnJobStopped();
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_not_cancel_the_work() => _jobStep.PerformToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_begin_stopping_the_run() => _jobStep.CurrentRun!.IsDraining.ShouldBeFalse();

    async Task Destroy()
    {
        _jobStep.Gate.TrySetResult();
        await _jobStep.WorkCompleted.Task;
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
