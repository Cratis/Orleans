// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class a_worker_dies_during_a_job : Specification
{
    PlacementClusterFixture _fixture;
    JobState _state;
    SiloAddress _controllerBefore;
    SiloAddress _controllerAfter;
    List<SiloAddress> _entriesAfterTheDeath;

    void Establish() => _fixture = new(new Dictionary<string, SiloPlan>
    {
        ["Primary"] = new(SiloPlan.ControllerRole, 1),
        ["Secondary_1"] = new(SiloPlan.ControllerRole, 2),
        ["Secondary_2"] = new(SiloPlan.WorkerRole, 2),
        ["Secondary_3"] = new(SiloPlan.WorkerRole, 1)
    });

    async Task Because()
    {
        await _fixture.WaitUntilMetadataIsVisible();
        var job = await _fixture.Start(2);
        await _fixture.Eventually(() => _fixture.Signals.Entries.Count == 2);
        _controllerBefore = await job.Silo();
        var before = _fixture.Signals.Entries.Count;
        await _fixture.Kill("Secondary_2");
        await _fixture.Eventually(() => _fixture.Signals.Entries.Count > before);
        _fixture.Signals.ReleaseAll();
        await _fixture.Signals.Completed.Task.WaitAsync(PlacementClusterFixture.Deadline, TimeProvider.System);
        _controllerAfter = await job.Silo();
        _entriesAfterTheDeath = _fixture.Signals.Entries.Skip(before).Select(entry => entry.Silo).ToList();
        _state = await job.Inspect();
    }

    [Fact] void should_move_the_steps_to_the_remaining_worker() => _entriesAfterTheDeath.TrueForAll(silo => silo.Equals(_fixture.AddressOf("Secondary_3"))).ShouldBeTrue();
    [Fact] void should_keep_the_job_on_its_controller() => _controllerAfter.ShouldEqual(_controllerBefore);
    [Fact] void should_complete_the_job() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    void Destroy() => _fixture.Dispose();
}
