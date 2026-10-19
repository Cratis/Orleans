// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_removal_and_the_job_rejects_the_stop_report_once : given.a_job_step_with_a_gated_throttle
{
    Result<JobStepError> _stopped;
    bool _removedBeforeTheSecondReport;

    protected override JobsOptions CreateOptions() => new() { StepCheckpointBatchInterval = 1, StepStopTimeout = StopTimeout, StepDeliveryRetryDelay = TimeSpan.FromMilliseconds(1) };

    async Task Establish()
    {
        var reports = 0;
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            if (++reports == 1)
            {
                return Result<JobError>.Failed(JobError.PersistStateError);
            }
            _removedBeforeTheSecondReport = _storage.Persisted.Any(_ => _.Status == JobStepStatus.Removing);
            return Result<JobError>.Success();
        });
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _stopped = await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_report_the_stop_again() => await _jobStep.ReportedJob.Received(2).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_keep_the_step_until_the_job_stored_the_stop() => _removedBeforeTheSecondReport.ShouldBeFalse();
    [Fact] void should_remove_the_step_once_the_job_stored_the_stop() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Removing);
}
