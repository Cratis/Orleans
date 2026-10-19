// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_silo_dies_while_a_job_is_stopping : Specification
{
    RecoveryClusterFixture _fixture;
    bool _stoppedBeforeTheSiloDied;
    JobState _state;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (_, job) = await _fixture.Start();
        (await job.Stop()).IsSuccess.ShouldBeTrue();

        // The performer ignores cancellation and its gate is never opened, so the step cannot report that it stopped.
        (await job.Inspect()).Status.ShouldEqual(JobStatus.Stopped);
        _stoppedBeforeTheSiloDied = _fixture.Signals.Stopped.Task.IsCompleted;
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
        await _fixture.Signals.Stopped.Task.WaitAsync(deadline.Token);
        _state = await job.Inspect();
    }

    [Fact] void should_not_have_finished_the_stop_before_the_silo_died() => _stoppedBeforeTheSiloDied.ShouldBeFalse();
    [Fact] void should_run_the_stopped_hook_after_recovery() => _fixture.Signals.StoppedCalls.ShouldEqual(1);
    [Fact] void should_keep_the_job_stopped() => _state.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_count_the_step_as_stopped() => _state.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_perform_the_step_again() => _fixture.Signals.Performers.Count.ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
