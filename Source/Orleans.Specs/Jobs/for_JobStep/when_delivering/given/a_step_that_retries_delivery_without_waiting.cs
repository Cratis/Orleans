// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering.given;

public class a_step_that_retries_delivery_without_waiting : for_JobStep.given.a_job_step_left_scheduled_by_a_dead_process
{
    protected GrainId _jobGrainId;

    protected virtual int DeliveryAttempts => 5;

    protected override JobsOptions CreateOptions() => new()
    {
        StepCheckpointBatchInterval = 1,
        StepStopTimeout = StopTimeout,
        StepDeliveryMaxAttempts = DeliveryAttempts,
        StepDeliveryRetryDelay = TimeSpan.Zero,
        MaxStepDeliveryRetryDelay = TimeSpan.Zero
    };

    protected async Task RunToDelivery()
    {
        _jobGrainId = GrainId.Create("job", _jobId.ToString());
        (await _jobStep.Start(_jobGrainId)).IsSuccess.ShouldBeTrue();
        await _jobStep.CurrentRun!.WorkerSettled.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Destroy() => _jobStep.Dispose();
}
