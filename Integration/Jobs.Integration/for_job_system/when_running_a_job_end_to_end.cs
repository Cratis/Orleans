// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// End-to-end spec for the job system against a real co-hosted silo and a real MongoDB: the job starts, its
/// steps run as real grains, the state lands in storage. Requires a MongoDB running on localhost:27017.
/// </summary>
/// <remarks>
/// The silo is shared by every fact of the class - one deployment, one job per fact.
/// </remarks>
[Collection(JobsClusterCollection.Name)]
public class when_running_a_job_end_to_end : Specification
{
    JobsClusterFixture _fixture = null!;
    JobId _jobId;
    JobState? _jobState;

    void Establish()
    {
        _fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
    }

    async Task Because()
    {
        var manager = _fixture.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty);
        var result = await manager.Start<IIntegrationJob, IntegrationJobRequest>(
            new IntegrationJobRequest(["first", "second", "third"]));
        _jobId = result.Match(id => id, _ => throw new Exception("The job could not be started"));

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var jobs = await manager.GetAllJobs();
            _jobState = jobs.FirstOrDefault(_ => _.Id == _jobId);
            if (_jobState is { Status: JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures })
            {
                break;
            }

            await Task.Delay(100);
        }
    }

    [Fact] void should_complete_the_job_with_all_steps_and_persisted_state()
    {
        _jobId.Value.ShouldNotEqual(Guid.Empty);
        _jobState.ShouldNotBeNull();
        _jobState!.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
        _jobState.Progress.SuccessfulSteps.ShouldEqual(3);
    }
}
