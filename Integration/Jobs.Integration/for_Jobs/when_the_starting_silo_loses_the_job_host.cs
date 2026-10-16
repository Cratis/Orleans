// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;

namespace Cratis.Orleans.Jobs.Integration.for_Jobs;

[Collection(JobsClusterCollection.Name)]
public class when_the_starting_silo_loses_the_job_host : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;
    SiloAddress _survivor;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (_, job) = await _fixture.Start(throughLocalApi: true);
        await _fixture.Kill();
        _survivor = await _fixture.Signals.SecondEntry.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _fixture.Signals.ReleaseRecovered.TrySetResult();
        await _fixture.Signals.Completed.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _state = await job.Inspect();
    }

    [Fact] void should_resume_on_a_surviving_silo() => _survivor.ShouldNotEqual(_fixture.VictimAddress);
    [Fact] void should_complete_the_original_job() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_finish_its_step() => _state.Progress.SuccessfulSteps.ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
