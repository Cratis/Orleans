// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// End-to-end spec for a job with a failing step, against a real co-hosted silo and a real MongoDB.
/// Requires a MongoDB running on localhost:27017.
/// </summary>
/// <remarks>
/// A failed step is the one a caller most needs to read afterwards - it carries why the job did not finish.
/// The storage moves failed steps into a collection of their own, so an unfiltered read has to look in both
/// places or the step simply vanishes, which is indistinguishable from a job that never had any steps.
/// That is a failure mode no unit spec sees, because it only exists once a real driver and two real
/// collections are involved.
/// </remarks>
[Collection(JobsClusterCollection.Name)]
public class when_a_job_step_fails : Specification
{
    JobsClusterFixture _fixture = null!;
    JobId _jobId;
    JobState? _jobState;
    IImmutableList<JobStepState> _jobSteps = [];
    IImmutableList<JobStepState> _failedJobSteps = [];

    void Establish()
    {
        _fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
    }

    async Task Because()
    {
        var manager = _fixture.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty);
        var result = await manager.Start<IIntegrationJob, IntegrationJobRequest>(
            new IntegrationJobRequest(["first", IntegrationJobStep.FailingItem]));
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

        var jobSteps = _fixture.Services.GetRequiredService<IJobsStorage>()
            .GetFor(JobsClusterFixture.Scope, string.Empty).JobSteps;
        _jobSteps = Unwrap(await jobSteps.GetForJob(_jobId));
        _failedJobSteps = Unwrap(await jobSteps.GetForJob(_jobId, JobStepStatus.CompletedWithFailure));
    }

    static IImmutableList<JobStepState> Unwrap(Cratis.Monads.Catch<IImmutableList<JobStepState>> result) =>
        result.Match(steps => steps, exception => throw new Exception("Reading job steps failed", exception));

    [Fact] void should_complete_the_job_with_failures() => _jobState!.Status.ShouldEqual(JobStatus.CompletedWithFailures);

    [Fact] void should_keep_the_failed_step() => _failedJobSteps.Count.ShouldEqual(1);

    [Fact] void should_return_the_failed_step_from_an_unfiltered_read() =>
        _jobSteps.Count(_ => _.Status == JobStepStatus.CompletedWithFailure).ShouldEqual(1);

    [Fact] void should_not_keep_the_successful_step() =>
        _jobSteps.Count(_ => _.Status == JobStepStatus.CompletedSuccessfully).ShouldEqual(0);
}
