// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_checkpoint_blocks_past_the_deadline : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _checkpointWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<JobStepError> _stopped;
    int _writesWhenTheStopGaveUp;

    protected override TimeSpan StopTimeout => TimeSpan.FromMilliseconds(100);

    async Task Establish()
    {
        _jobStep.Work = async (step, _) =>
        {
            _storage.HoldWrites = _checkpointWrite;
            await step.OnActivation(() => step.Checkpoint("blocked"));
        };
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _storage.WriteHeld.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _stopped = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _writesWhenTheStopGaveUp = _storage.Persisted.Count;
        _storage.HoldWrites = null;
        _checkpointWrite.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_answer_that_the_stop_timed_out() => ((JobStepError)_stopped).ShouldEqual(JobStepError.StopTimedOut);
    [Fact] void should_let_the_started_checkpoint_land_first() => _storage.Persisted[_writesWhenTheStopGaveUp].ShouldEqual(new given.PersistedStep(JobStepStatus.Running, "blocked"));
    [Fact] void should_record_the_stop_after_the_checkpoint() => _storage.Persisted.Skip(_writesWhenTheStopGaveUp + 1).Select(_ => _.Status).ShouldContainOnly([JobStepStatus.Stopped]);
    [Fact] async Task should_report_the_stop_once_the_work_settles() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());

    void Destroy()
    {
        _checkpointWrite.TrySetResult();
        _jobStep.Dispose();
    }
}
