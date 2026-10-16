// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_hosting_a_running_job_dies : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;
    ResumeJobSuccess _resume;
    SiloAddress _survivor;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (_, job) = await _fixture.Start();
        await _fixture.Kill();

        // No application Resume/Rehydrate: membership-triggered recovery must re-enter the performer.
        _survivor = await _fixture.Signals.SecondEntry.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _fixture.Signals.ReleaseRecovered.TrySetResult();
        await _fixture.Signals.Completed.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);

        // Inspect queues behind the non-reentrant completion turn, including its durable status write.
        _state = await job.Inspect();
        _resume = (ResumeJobSuccess)await job.Resume();
    }

    [Fact] void should_recover_the_original_job_once_on_a_surviving_silo()
    {
        _survivor.ShouldNotEqual(_fixture.VictimAddress);
        _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
        _state.Progress.SuccessfulSteps.ShouldEqual(1);
        _state.Progress.TotalSteps.ShouldEqual(1);
        _resume.ShouldEqual(ResumeJobSuccess.JobIsCompleted);
        _fixture.Signals.Performers.Count.ShouldEqual(2);
        _fixture.Signals.ReleaseOriginal.Task.IsCompleted.ShouldBeFalse();
    }

    void Destroy() => _fixture.Dispose();
}
