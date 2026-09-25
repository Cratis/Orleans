// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Stages;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_job_system.when_running_a_job_in_stages.given;

/// <summary>
/// Runs a <see cref="StagedIntegrationJob"/> to completion against a real co-hosted silo and a real MongoDB.
/// Requires a MongoDB running on localhost:27017.
/// </summary>
public class a_staged_job : Specification
{
    protected JobsClusterFixture _fixture = null!;
    protected JobId _jobId;
    protected JobState? _jobState;
    protected IImmutableList<JobStepState> _jobSteps = [];

    protected async Task Run(params IReadOnlyList<string>[] stages)
    {
        var manager = _fixture.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty);
        var result = await manager.Start<IStagedIntegrationJob, StagedIntegrationJobRequest>(new StagedIntegrationJobRequest(stages));
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

        var jobSteps = _fixture.Services.GetRequiredService<IJobsStorage>().GetFor(JobsClusterFixture.Scope, string.Empty).JobSteps;
        _jobSteps = (await jobSteps.GetForJob(_jobId)).Match(steps => steps, exception => throw new Exception("Reading job steps failed", exception));
    }

    void Establish()
    {
        _fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
    }
}
