// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_hosting_a_stopped_job_dies : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (_, job) = await _fixture.Start();
        (await job.Stop()).IsSuccess.ShouldBeTrue();
        _fixture.Signals.ReleaseOriginal.TrySetResult();
        await _fixture.Signals.Stopped.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        (await job.Inspect()).Status.ShouldEqual(JobStatus.Stopped);
        while (_fixture.Signals.RecoveryPasses.Wait(0))
        {
        }
        await _fixture.Kill();
        var membership = ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services.GetRequiredService<IClusterMembershipService>();
        using var deadline = new CancellationTokenSource(RecoveryClusterFixture.Deadline);
        await foreach (var snapshot in membership.MembershipUpdates.WithCancellation(deadline.Token))
        {
            if (snapshot.GetSiloStatus(_fixture.VictimAddress) is SiloStatus.Dead)
            {
                break;
            }
        }
        await _fixture.Signals.RecoveryPasses.WaitAsync(deadline.Token);
        _state = await job.Inspect();
    }

    [Fact] void should_preserve_the_durable_stop_during_automatic_recovery()
    {
        _state.Status.ShouldEqual(JobStatus.Stopped);
        _state.Progress.StoppedSteps.ShouldEqual(1);
        _fixture.Signals.Performers.Count.ShouldEqual(1);
        _fixture.Signals.SecondEntry.Task.IsCompleted.ShouldBeFalse();
    }

    void Destroy() => _fixture.Dispose();
}
