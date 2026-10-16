// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_stopping_a_job_while_its_step_is_performing : Specification
{
    DrainingClusterFixture _fixture;
    Result<StopJobError> _stopped;
    JobState _jobState;
    JobStepState _stepState;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var jobId = await _fixture.StartAndWaitForTheStepToPerform();
        var job = _fixture.GetJob(jobId);
        _stopped = await job.Stop().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        await _fixture.Signals.JobStopped.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        await _fixture.Signals.StopPersisted.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // Inspect queues behind the non-reentrant turn that recorded the step's stop.
        _jobState = await job.Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _stepState = await _fixture.GetStepState(jobId);
    }

    [Fact] void should_accept_the_stop() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_stop_the_job() => _jobState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_finish_stopping_the_job() => _jobState.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_count_one_stopped_step() => _jobState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_persist_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_record_the_stop_of_the_step_once() => _stepState.StatusChanges.Count(change => change.Status == JobStepStatus.Stopped).ShouldEqual(1);
    [Fact] void should_let_the_worker_observe_the_stop() => _fixture.Signals.Cancelled.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_persist_the_final_checkpoint() => _stepState.Progress.Message.Value.ShouldEqual(DrainingStep.FinalCheckpointMessage);

    void Destroy() => _fixture.Dispose();
}
