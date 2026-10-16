// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_hosting_a_draining_job_dies : Specification
{
    DrainingClusterFixture _fixture;
    DrainJobResult _beforeTheCrash;
    JobState _stateBeforeTheCrash;
    DrainJobResult _result;
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
        _stateBeforeTheCrash = await _fixture.GetJob(_jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        await _fixture.KillHost();
        _result = await _fixture.Jobs.StopAndDrain(_fixture.Key, _jobId, TimeSpan.FromSeconds(10)).WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _jobState = await _fixture.GetJob(_jobId).Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _stepState = await _fixture.GetStepState(_jobId);
    }

    [Fact] void should_not_be_drained_before_the_crash() => _beforeTheCrash.ShouldEqual(new DrainJobResult.TimedOut(_jobId));
    [Fact] void should_have_persisted_the_pending_drain_before_the_crash() => _stateBeforeTheCrash.DrainPending.ShouldBeTrue();
    [Fact] void should_have_persisted_the_stop_before_the_crash() => _stateBeforeTheCrash.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_complete_the_drain_after_the_crash() => _result.ShouldEqual(new DrainJobResult.Drained(_jobId));
    [Fact] void should_leave_the_job_stopped() => _jobState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_have_settled_the_drain() => _jobState.DrainPending.ShouldBeFalse();
    [Fact] void should_have_persisted_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_start_the_step_again() => _fixture.Signals.Entries.ShouldEqual(1);

    void Destroy()
    {
        _fixture.Signals.ReleaseWork.TrySetResult();
        _fixture.Dispose();
    }
}
