// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class placing_controllers_and_workers_separately : Specification
{
    PlacementClusterFixture _fixture;
    JobState _state;
    SiloAddress _jobSilo;

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
        _jobSilo = await job.Silo();
        _fixture.Signals.ReleaseAll();
        await _fixture.Signals.Completed.Task.WaitAsync(PlacementClusterFixture.Deadline, TimeProvider.System);
        _state = await job.Inspect();
    }

    [Fact] void should_place_the_job_on_the_preferred_controller() => _jobSilo.ShouldEqual(_fixture.AddressOf("Secondary_1"));
    [Fact] void should_place_every_step_on_the_preferred_worker() => _fixture.Signals.EntriesOn().All(silo => silo.Equals(_fixture.AddressOf("Secondary_2"))).ShouldBeTrue();
    [Fact] void should_complete_the_job() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    void Destroy() => _fixture.Dispose();
}
