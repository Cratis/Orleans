// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_dies_while_resuming_a_stopped_job : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;
    SiloAddress _survivor;

    void Establish()
    {
        _fixture = new();
        _fixture.Signals.HoldFirstResume = true;
    }

    async Task Because()
    {
        var (id, job) = await _fixture.Start();
        (await job.Stop()).IsSuccess.ShouldBeTrue();
        _fixture.Signals.ReleaseOriginal.TrySetResult();
        await _fixture.Signals.StepStopped.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        (await job.Inspect()).Status.ShouldEqual(JobStatus.Stopped);
        var storage = ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services.GetRequiredService<IJobsStorage>()
            .GetFor(_fixture.Signals.Key.Scope, _fixture.Signals.Key.Namespace);
        (await storage.JobSteps.GetForJob(id)).AsT0.Single().Status.ShouldEqual(JobStepStatus.Stopped);

        var interruptedResume = job.Resume();
        await _fixture.Signals.ResumeEntered.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        var interruptedState = (await storage.Jobs.GetJob(id)).AsT0;
        interruptedState.Status.ShouldEqual(JobStatus.Running);
        interruptedState.Progress.StoppedSteps.ShouldEqual(0);
        (await storage.JobSteps.GetForJob(id)).AsT0.Single().Status.ShouldEqual(JobStepStatus.Stopped);
        await _fixture.Kill();

        // The caller loses its in-flight request; only automatic membership recovery restarts the step.
        _ = await Specifications.Catch.Exception(async () => await interruptedResume.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System));
        _survivor = await _fixture.Signals.SecondEntry.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _fixture.Signals.ReleaseRecovered.TrySetResult();
        await _fixture.Signals.Completed.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _state = await job.Inspect();
    }

    [Fact] void should_recover_the_interrupted_resume_on_a_surviving_silo()
    {
        _survivor.ShouldNotEqual(_fixture.VictimAddress);
        _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
        _state.Progress.SuccessfulSteps.ShouldEqual(1);
        _state.Progress.TotalSteps.ShouldEqual(1);
        _state.Progress.StoppedSteps.ShouldEqual(0);
        _fixture.Signals.Performers.Count.ShouldEqual(2);
        _fixture.Signals.ReleaseResume.Task.IsCompleted.ShouldBeFalse();
    }

    void Destroy() => _fixture.Dispose();
}
