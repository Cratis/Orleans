// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_draining_a_job_while_its_step_is_in_a_callback : Specification
{
    DrainingClusterFixture _fixture;
    DrainJobResult _result;
    bool _drainedWhileInTheCallback;
    bool _jobStoppedHookRanBeforeTheAnswer;
    bool _stepHookRanBeforeTheAnswer;
    int _writesAtTheAnswer;
    JobId _jobId;
    JobState _jobState;
    JobStepState _stepState;

    void Establish()
    {
        // The step runs on another silo than its job, and holds its final checkpoint callback open on its activation.
        _fixture = new(stepOnAnotherSilo: true);
        _fixture.Signals.HoldInFinalCheckpoint = true;
    }

    async Task Because()
    {
        _jobId = await _fixture.StartAndWaitForTheStepToPerform();
        var draining = _fixture.Jobs.StopAndDrain(_fixture.Key, _jobId, TimeSpan.FromSeconds(10));
        await _fixture.Signals.InFinalCheckpoint.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _drainedWhileInTheCallback = draining.IsCompleted;
        _fixture.Signals.ReleaseWork.TrySetResult();
        _result = await draining.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _writesAtTheAnswer = _fixture.Signals.Writes;
        _jobStoppedHookRanBeforeTheAnswer = _fixture.Signals.JobStopped.Task.IsCompleted;
        _stepHookRanBeforeTheAnswer = _fixture.Signals.StepOutcomeRecorded.Task.IsCompleted;

        // The step deactivates once its own report of the outcome is done, which is after anything it could write.
        await _fixture.Signals.Deactivated.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _jobState = await _fixture.GetJob(_jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _stepState = await _fixture.GetStepState(_jobId);
    }

    [Fact] void should_be_drained() => _result.ShouldEqual(new DrainJobResult.Drained(_jobId));
    [Fact] void should_not_answer_while_the_callback_runs() => _drainedWhileInTheCallback.ShouldBeFalse();
    [Fact] void should_have_persisted_the_final_checkpoint() => _stepState.Progress.Message.Value.ShouldEqual(DrainingStep.FinalCheckpointMessage);
    [Fact] void should_have_persisted_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_record_the_stop_of_the_step_once() => _stepState.StatusChanges.Count(_ => _.Status == JobStepStatus.Stopped).ShouldEqual(1);
    [Fact] void should_have_run_the_step_hook_before_answering() => _stepHookRanBeforeTheAnswer.ShouldBeTrue();
    [Fact] void should_have_run_the_stopped_hook_before_answering() => _jobStoppedHookRanBeforeTheAnswer.ShouldBeTrue();
    [Fact] void should_not_write_step_state_after_answering() => _fixture.Signals.Writes.ShouldEqual(_writesAtTheAnswer);
    [Fact] void should_not_perform_the_step_again() => _fixture.Signals.Entries.ShouldEqual(1);
    [Fact] void should_leave_the_job_stopped() => _jobState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_have_settled_the_drain() => _jobState.DrainPending.ShouldBeFalse();
    [Fact] void should_count_the_stopped_step_once() => _jobState.Progress.StoppedSteps.ShouldEqual(1);

    void Destroy()
    {
        _fixture.Signals.ReleaseWork.TrySetResult();
        _fixture.Dispose();
    }
}
