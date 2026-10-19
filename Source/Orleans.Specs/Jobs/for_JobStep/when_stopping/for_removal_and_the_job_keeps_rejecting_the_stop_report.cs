// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_removal_and_the_job_keeps_rejecting_the_stop_report : given.a_job_step_with_a_gated_throttle
{
    const int Rejections = 15;
    readonly TaskCompletionSource _rejectedEnough = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _removedWhileRejected;

    protected override JobsOptions CreateOptions() => new()
    {
        StepCheckpointBatchInterval = 1,
        StepStopTimeout = StopTimeout,
        StepDeliveryMaxAttempts = 2,
        StepDeliveryRetryDelay = TimeSpan.FromMilliseconds(1),
        MaxStepDeliveryRetryDelay = TimeSpan.FromMilliseconds(1)
    };

    async Task Establish()
    {
        var reports = 0;
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            if (++reports <= Rejections)
            {
                return Result<JobError>.Failed(JobError.PersistStateError);
            }
            _removedWhileRejected = _storage.Persisted.Any(_ => _.Status == JobStepStatus.Removing);
            _rejectedEnough.TrySetResult();
            return Result<JobError>.Success();
        });
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_report_past_the_delivery_attempts() => _rejectedEnough.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_keep_the_step_while_the_job_rejects_the_stop() => _removedWhileRejected.ShouldBeFalse();
    [Fact] void should_remove_the_step_once_the_job_stored_the_stop() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Removing);
}
