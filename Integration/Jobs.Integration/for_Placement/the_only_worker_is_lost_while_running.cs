// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class the_only_worker_is_lost_while_running : Specification
{
    PlacementClusterFixture _fixture;
    JobState _whileWithoutWorker;
    JobState _state;
    SiloAddress _newWorker;
    SiloAddress _stepSilo;
    int _callsInWindow;
    double _windowSeconds;

    void Establish() => _fixture = new(
        new Dictionary<string, SiloPlan>
        {
            ["Primary"] = new(SiloPlan.ControllerRole, 1),
            ["Secondary_1"] = new(SiloPlan.ControllerRole, 1),
            ["Secondary_2"] = new(SiloPlan.WorkerRole, 1)
        },
        extras: [new SiloPlan(SiloPlan.WorkerRole, 1)]);

    async Task Because()
    {
        await _fixture.WaitUntilMetadataIsVisible();
        var job = await _fixture.Start(1);
        await _fixture.Eventually(() => _fixture.Signals.Entries.Count == 1);
        var callsBefore = _fixture.Signals.WorkerDirectorCalls;
        var started = DateTime.UtcNow;
        await _fixture.Kill("Secondary_2");
        await Task.Delay(TimeSpan.FromSeconds(5));
        _whileWithoutWorker = await job.Inspect();
        _windowSeconds = (DateTime.UtcNow - started).TotalSeconds;
        _callsInWindow = _fixture.Signals.WorkerDirectorCalls - callsBefore;

        _newWorker = await _fixture.StartWorker();
        await _fixture.WaitUntilMetadataIsVisible();
        await _fixture.Eventually(() => _fixture.Signals.Entries.Any(entry => entry.Silo.Equals(_newWorker)));
        _stepSilo = _fixture.Signals.Entries.Last().Silo;
        _fixture.Signals.ReleaseAll();
        await _fixture.Signals.Completed.Task.WaitAsync(PlacementClusterFixture.Deadline, TimeProvider.System);
        _state = await job.Inspect();
    }

    [Fact] void should_keep_the_job_running_without_a_worker() => _whileWithoutWorker.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_fail_the_step_without_a_worker() => _whileWithoutWorker.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_retry_at_most_once_per_watchdog_interval() => (_callsInWindow <= _windowSeconds + 3).ShouldBeTrue();
    [Fact] void should_run_the_step_on_the_new_worker() => _stepSilo.ShouldEqual(_newWorker);
    [Fact] void should_complete_the_job() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_count_one_success() => _state.Progress.SuccessfulSteps.ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
