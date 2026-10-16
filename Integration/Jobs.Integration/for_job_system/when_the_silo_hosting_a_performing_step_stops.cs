// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_hosting_a_performing_step_stops : Specification
{
    DrainingClusterFixture _fixture;
    TimeSpan _stopping;
    bool _siloStoppedWhileTheWorkerRan;
    bool _workerSettledBeforeTheSiloStopped;
    int _writesWhenTheSiloStopped;
    int _activationsWhenTheSiloStopped;
    int _writesAfterwards;
    JobStepState _stepState;
    Result<ActivationCallbackRejected> _finalCheckpoint;

    void Establish()
    {
        _fixture = new();

        // The work keeps running after it observed cancellation until the spec releases it.
        _fixture.Signals.HoldAfterCancellation = true;
    }

    async Task Because()
    {
        var jobId = await _fixture.StartAndWaitForTheStepToPerform();
        var stopwatch = Stopwatch.StartNew();
        var stopping = _fixture.StopHost();
        await _fixture.Signals.Cancelled.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _siloStoppedWhileTheWorkerRan = stopping.IsCompleted;
        _fixture.Signals.ReleaseWork.TrySetResult();
        await stopping;
        _stopping = stopwatch.Elapsed;
        _workerSettledBeforeTheSiloStopped = _fixture.Signals.WorkerSettled.Task.IsCompleted;
        _writesWhenTheSiloStopped = _fixture.Signals.Writes;
        _activationsWhenTheSiloStopped = _fixture.Signals.Activations;

        // Read back through storage after the stop; a write or activation rerouted from the stopped silo would
        // otherwise show up here.
        _stepState = await _fixture.GetStepState(jobId);
        _finalCheckpoint = await _fixture.Signals.FinalCheckpoint.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _writesAfterwards = _fixture.Signals.Writes;
    }

    [Fact] void should_not_finish_stopping_the_silo_while_the_worker_runs() => _siloStoppedWhileTheWorkerRan.ShouldBeFalse();
    [Fact] void should_let_the_worker_settle_before_the_silo_has_stopped() => _workerSettledBeforeTheSiloStopped.ShouldBeTrue();
    [Fact] void should_stop_the_silo_within_the_deactivation_timeout() => _stopping.ShouldBeLessThan(DrainingClusterConfigurator.DeactivationTimeout);
    [Fact] void should_not_write_step_state_after_the_silo_stopped() => _writesAfterwards.ShouldEqual(_writesWhenTheSiloStopped);
    [Fact] void should_admit_the_final_checkpoint_while_deactivating() => _finalCheckpoint.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_final_checkpoint() => _stepState.Progress.Message.Value.ShouldEqual(DrainingStep.FinalCheckpointMessage);
    [Fact] void should_leave_the_step_running_for_recovery() => _stepState.Status.ShouldEqual(JobStepStatus.Running);
    [Fact] void should_not_activate_the_step_a_second_time() => _fixture.Signals.Activations.ShouldEqual(1);
    [Fact] void should_have_activated_the_step_once_when_the_silo_stopped() => _activationsWhenTheSiloStopped.ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
