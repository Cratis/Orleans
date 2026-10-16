// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// Nobody asks for the drain again after the job's silo died: a call that only reads the job activates it, and the
/// activation completes the pending drain on its own.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class when_a_draining_job_is_activated_after_its_silo_died : Specification
{
    DrainingClusterFixture _fixture;
    DrainJobResult _beforeTheCrash;
    JobId _jobId;
    JobState _jobState;
    JobStepState _stepState;

    void Establish()
    {
        // The step holds on after it saw the cancellation, so the first drain cannot finish.
        _fixture = new();
        _fixture.Signals.HoldAfterCancellation = true;
    }

    async Task Because()
    {
        _jobId = await _fixture.StartAndWaitForTheStepToPerform();
        _beforeTheCrash = await _fixture.Jobs.StopAndDrain(_fixture.Key, _jobId, TimeSpan.Zero).WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        await _fixture.KillHost();
        _ = await _fixture.GetJob(_jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // The stopped hook runs only when the drain settles, so it signals that the activation completed it.
        await _fixture.Signals.JobStopped.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _jobState = await _fixture.GetJob(_jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _stepState = await _fixture.GetStepState(_jobId);
    }

    [Fact] void should_not_be_drained_before_the_crash() => _beforeTheCrash.ShouldEqual(new DrainJobResult.TimedOut(_jobId));
    [Fact] void should_have_settled_the_drain() => _jobState.DrainPending.ShouldBeFalse();
    [Fact] void should_leave_the_job_stopped() => _jobState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_have_persisted_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_start_the_step_again() => _fixture.Signals.Entries.ShouldEqual(1);

    void Destroy()
    {
        _fixture.Signals.ReleaseWork.TrySetResult();
        _fixture.Dispose();
    }
}
