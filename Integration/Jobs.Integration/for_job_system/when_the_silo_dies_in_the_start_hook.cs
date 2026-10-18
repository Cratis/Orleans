// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_dies_in_the_start_hook : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;
    JobStepId _plannedId;
    SiloAddress _survivor;

    void Establish()
    {
        _fixture = new();
        _fixture.Signals.HoldFirstStart = true;
    }

    async Task Because()
    {
        var id = JobId.New();
        var interruptedStart = _fixture.Manager.Start<IRecoveryJob, RecoveryRequest>(id, new());
        await _fixture.Signals.StartEntered.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        var storage = ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services.GetRequiredService<IJobsStorage>()
            .GetFor(_fixture.Signals.Key.Scope, _fixture.Signals.Key.Namespace);
        var saved = (await storage.Jobs.GetJob(id)).AsT0;
        saved.Status.ShouldEqual(JobStatus.StartingSteps);
        _plannedId = saved.StepPlan!.Single().Id;
        (await storage.JobSteps.GetForJob(id)).AsT0.Single().IsPrepared.ShouldBeTrue();
        _fixture.Signals.Performers.ShouldBeEmpty();
        await _fixture.Kill();
        _ = await Specifications.Catch.Exception(async () => await interruptedStart.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System));

        _survivor = await _fixture.Signals.FirstEntry.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _fixture.Signals.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
        _fixture.Signals.ReleaseOriginal.TrySetResult();
        await _fixture.Signals.Completed.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        var job = _fixture.Cluster.GrainFactory.GetGrain<IRecoveryJob>(id, new JobKey(_fixture.Signals.Key.Scope, _fixture.Signals.Key.Namespace));
        _state = await job.Inspect();
    }

    [Fact] void should_recover_without_replanning_or_replaying_the_start_hook()
    {
        _survivor.ShouldNotEqual(_fixture.VictimAddress);
        _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
        _state.StepPlan!.Single().Id.ShouldEqual(_plannedId);
        _state.Progress.SuccessfulSteps.ShouldEqual(1);
        _fixture.Signals.PlanCalls.ShouldEqual(1);
        _fixture.Signals.StartHookCalls.ShouldEqual(1);
        _fixture.Signals.Performers.Count.ShouldEqual(1);
        _fixture.Signals.ReleaseStart.Task.IsCompleted.ShouldBeFalse();
    }

    void Destroy() => _fixture.Dispose();
}
