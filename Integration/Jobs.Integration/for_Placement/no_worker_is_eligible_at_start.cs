// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Placement;

namespace Cratis.Orleans.Jobs.Integration.for_Placement;

[Collection(JobsClusterCollection.Name)]
public class no_worker_is_eligible_at_start : Specification
{
    PlacementClusterFixture _fixture;
    JobState _failed;
    JobState _later;
    int _directorCallsWhenFailed;

    void Establish() => _fixture = new(new Dictionary<string, SiloPlan>
    {
        ["Primary"] = new(SiloPlan.ControllerRole, 1),
        ["Secondary_1"] = new(SiloPlan.ControllerRole, 1),
        ["Secondary_2"] = new(SiloPlan.ControllerRole, 1)
    });

    async Task Because()
    {
        await _fixture.WaitUntilMetadataIsVisible();
        var (id, _) = await _fixture.TryStart(1);
        var job = _fixture.JobOf(id);
        await _fixture.Eventually(() => StatusIs(job, JobStatus.Failed));
        _failed = await job.Inspect();
        _directorCallsWhenFailed = _fixture.Signals.WorkerDirectorCalls;

        // Three watchdog intervals with margin: a failed job must not be retried.
        await Task.Delay(TimeSpan.FromSeconds(4));
        _later = await job.Inspect();
    }

    [Fact] void should_fail_the_job() => _failed.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_say_why() => _failed.StatusChanges[^1].ExceptionMessages.Any(message => message.Contains("No silo is eligible")).ShouldBeTrue();
    [Fact] void should_not_run_any_step() => _fixture.Signals.Entries.Count.ShouldEqual(0);
    [Fact] void should_ask_for_a_worker_once() => _directorCallsWhenFailed.ShouldEqual(1);
    [Fact] void should_not_ask_again() => _fixture.Signals.WorkerDirectorCalls.ShouldEqual(_directorCallsWhenFailed);
    [Fact] void should_keep_the_status() => _later.Status.ShouldEqual(_failed.Status);
    [Fact] void should_not_record_more_status_changes() => _later.StatusChanges.Count.ShouldEqual(_failed.StatusChanges.Count);

    void Destroy() => _fixture.Dispose();

    static bool StatusIs(IPlacementJob job, JobStatus status)
    {
        try
        {
            return job.Inspect().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System).GetAwaiter().GetResult().Status == status;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
