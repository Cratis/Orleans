// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// The same job the MongoDB specs run, against SQL storage and a real co-hosted silo.
/// </summary>
/// <remarks>
/// The SQL storage is reached through the grain-storage providers, and nothing was running that path - the
/// storage-level specs write through the storage directly, so they never see what the provider hands it. An
/// application on SQL could start a job, be told it started, and find nothing in the database afterwards.
/// </remarks>
[Collection(SqlJobsClusterCollection.Name)]
public class when_running_a_job_against_sql_storage : Specification
{
    SqlJobsClusterFixture _fixture = null!;
    JobId _jobId;
    JobState? _jobState;
    IImmutableList<JobState> _storedJobs = [];
    IImmutableList<JobStepState> _storedSteps = [];

    void Establish() => _fixture = SqlJobsClusterFixture.Shared;

    async Task Because()
    {
        var manager = _fixture.GrainFactory.GetJobsManager(SqlJobsClusterFixture.Scope, string.Empty);
        var result = await manager.Start<IIntegrationJob, IntegrationJobRequest>(
            new IntegrationJobRequest(["first", "second"]));
        _jobId = result.Match(id => id, error => throw new Exception($"The job could not be started: {error}"));

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var jobs = await manager.GetAllJobs();
            _jobState = jobs.FirstOrDefault(_ => _.Id == _jobId);
            if (_jobState is { Status: JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures })
            {
                break;
            }

            await Task.Delay(100).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        var storage = _fixture.Services.GetRequiredService<IJobsStorage>()
            .GetFor(SqlJobsClusterFixture.Scope, string.Empty);

        _storedJobs = Unwrap(await storage.Jobs.GetJobs());
        _storedSteps = Unwrap(await storage.JobSteps.GetForJob(_jobId));
    }

    static IImmutableList<T> Unwrap<T>(Cratis.Monads.Catch<IImmutableList<T>> result) =>
        result.Match(value => value, exception => throw exception);

    [Fact] void should_report_the_job_as_completed() => _jobState!.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    [Fact] void should_have_written_the_job_to_the_database() => _storedJobs.Any(_ => _.Id == _jobId).ShouldBeTrue();

    [Fact] void should_have_run_a_step_for_each_item() => _jobState!.Progress.SuccessfulSteps.ShouldEqual(2);

    /// <summary>
    /// A step that succeeded is removed when the job completes, so an empty read here is the storage doing
    /// what it should - and the reason the count above is taken from the job's own progress.
    /// </summary>
    [Fact] void should_not_keep_the_steps_that_succeeded() => _storedSteps.ShouldBeEmpty();
}
