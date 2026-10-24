// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class workers_have_different_throttle_limits : Specification
{
    PlacementClusterFixture _fixture;
    JobState _state;
    SiloAddress _small;
    SiloAddress _large;
    int _smallActive;
    int _largeActive;
    int _smallPeakWhileHeld;
    int _largePeakWhileHeld;

    void Establish() => _fixture = new(
        new Dictionary<string, SiloPlan>
        {
            ["Primary"] = new(SiloPlan.ControllerRole, 1),
            ["Secondary_1"] = new(SiloPlan.ControllerRole, 1),
            ["Secondary_2"] = new(SiloPlan.WorkerRole, 1, MaxParallelSteps: 1),
            ["Secondary_3"] = new(SiloPlan.WorkerRole, 1, MaxParallelSteps: 2)
        },
        roundRobinWorkers: true);

    async Task Because()
    {
        await _fixture.WaitUntilMetadataIsVisible();
        _small = _fixture.AddressOf("Secondary_2");
        _large = _fixture.AddressOf("Secondary_3");
        var job = await _fixture.Start(6);
        await _fixture.Eventually(() => _fixture.Signals.ActiveOn(_small) == 1 && _fixture.Signals.ActiveOn(_large) == 2);
        await Task.Delay(TimeSpan.FromSeconds(1));
        _smallActive = _fixture.Signals.ActiveOn(_small);
        _largeActive = _fixture.Signals.ActiveOn(_large);
        _smallPeakWhileHeld = _fixture.Signals.PeakOn(_small);
        _largePeakWhileHeld = _fixture.Signals.PeakOn(_large);
        _fixture.Signals.ReleaseSilo(_small);
        _fixture.Signals.ReleaseSilo(_large);
        await _fixture.Signals.Completed.Task.WaitAsync(PlacementClusterFixture.Deadline, TimeProvider.System);
        _state = await job.Inspect();
    }

    [Fact] void should_run_one_step_at_a_time_on_the_small_worker() => _smallActive.ShouldEqual(1);
    [Fact] void should_run_two_steps_at_a_time_on_the_large_worker() => _largeActive.ShouldEqual(2);
    [Fact] void should_never_exceed_the_small_limit_while_held() => _smallPeakWhileHeld.ShouldEqual(1);
    [Fact] void should_never_exceed_the_large_limit_while_held() => _largePeakWhileHeld.ShouldEqual(2);
    [Fact] void should_never_exceed_the_small_limit() => _fixture.Signals.PeakOn(_small).ShouldEqual(1);
    [Fact] void should_never_exceed_the_large_limit() => _fixture.Signals.PeakOn(_large).ShouldEqual(2);
    [Fact] void should_complete_every_step() => _state.Progress.SuccessfulSteps.ShouldEqual(6);

    void Destroy() => _fixture.Dispose();
}
