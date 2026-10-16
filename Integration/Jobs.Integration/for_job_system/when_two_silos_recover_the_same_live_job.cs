// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_two_silos_recover_the_same_live_job : Specification
{
    RecoveryClusterFixture _fixture;
    JobState _state;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (id, job) = await _fixture.Start();
        var factories = new[] { _fixture.Cluster.Primary, _fixture.Cluster.SecondarySilos[1] }
            .Cast<InProcessSiloHandle>()
            .Select(silo => silo.SiloHost.Services.GetRequiredService<IGrainFactory>()).ToArray();
        var key = _fixture.Signals.Key;

        // Disagreeing membership views may elect both silos. Dispatch the Resume calls from their
        // independent discovery snapshots concurrently to the same durable job/step identities.
        var resumes = await Task.WhenAll(factories.Select(factory => factory
            .GetGrain<IRecoveryJob>(id, new JobKey(key.Scope, key.Namespace)).Resume()));
        resumes.ShouldEachConformTo(result => result.IsSuccess && (ResumeJobSuccess)result == ResumeJobSuccess.JobAlreadyRunning);
        using var deadline = new CancellationTokenSource(RecoveryClusterFixture.Deadline);
        (await job.Inspect()).Progress.SuccessfulSteps.ShouldEqual(0);
        _fixture.Signals.Performers.Count.ShouldEqual(1);
        _fixture.Signals.ReleaseOriginal.TrySetResult();
        await _fixture.Signals.Completed.Task.WaitAsync(deadline.Token);
        _state = await job.Inspect();
    }

    [Fact] void should_not_duplicate_live_work_or_corrupt_completion()
    {
        _fixture.Signals.Performers.Count.ShouldEqual(1);
        _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
        _state.Progress.SuccessfulSteps.ShouldEqual(1);
        _state.Progress.StoppedSteps.ShouldEqual(0);
    }

    void Destroy() => _fixture.Dispose();
}
