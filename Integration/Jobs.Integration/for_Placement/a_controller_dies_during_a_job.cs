// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class a_controller_dies_during_a_job : Specification
{
    PlacementClusterFixture _fixture;
    JobState _state;
    SiloAddress _controllerAfter;
    List<SiloAddress> _entries;

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
        await _fixture.Kill("Secondary_1");
        await _fixture.Eventually(() => IsOnPrimary(job));
        _fixture.Signals.ReleaseAll();
        await _fixture.Signals.Completed.Task.WaitAsync(PlacementClusterFixture.Deadline, TimeProvider.System);
        _controllerAfter = await job.Silo();
        _entries = _fixture.Signals.EntriesOn().ToList();
        _state = await job.Inspect();
    }

    [Fact] void should_resume_the_job_on_the_remaining_controller() => _controllerAfter.ShouldEqual(_fixture.AddressOf("Primary"));
    [Fact] void should_never_run_a_step_on_a_controller() =>
        _entries.Exists(silo => silo.Equals(_fixture.AddressOf("Primary"))).ShouldBeFalse();
    [Fact] void should_complete_the_job() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    void Destroy() => _fixture.Dispose();

    bool IsOnPrimary(IPlacementJob job)
    {
        try
        {
            return job.Silo().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System).GetAwaiter().GetResult().Equals(_fixture.AddressOf("Primary"));
        }
        catch (Exception)
        {
            return false;
        }
    }
}
