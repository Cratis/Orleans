// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_stopping_a_performing_step_directly : Specification
{
    DrainingClusterFixture _fixture;
    Result<JobStepError> _stopped;
    bool _workerSettledWhenTheStopReturned;
    JobState _jobState;
    JobStepState _stepState;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var jobId = await _fixture.StartAndWaitForTheStepToPerform();
        var step = await _fixture.GetStep(jobId);
        _stopped = await step.Stop(false).WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _workerSettledWhenTheStopReturned = _fixture.Signals.WorkerSettled.Task.IsCompleted;
        await _fixture.Signals.StepOutcomeRecorded.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        await _fixture.Signals.StopPersisted.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // Inspect queues behind the non-reentrant turn that recorded the step's stop.
        _jobState = await _fixture.GetJob(jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _stepState = await _fixture.GetStepState(jobId);
    }

    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_let_the_worker_settle_before_returning() => _workerSettledWhenTheStopReturned.ShouldBeTrue();
    [Fact] void should_count_one_stopped_step_on_the_job() => _jobState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_persist_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_persist_the_final_checkpoint() => _stepState.Progress.Message.Value.ShouldEqual(DrainingStep.FinalCheckpointMessage);
    [Fact] void should_record_the_stop_of_the_step_once() => _stepState.StatusChanges.Count(change => change.Status == JobStepStatus.Stopped).ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
