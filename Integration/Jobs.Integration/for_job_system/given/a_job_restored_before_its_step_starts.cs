// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_job_system.given;

public class a_job_restored_before_its_step_starts : Specification
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    protected readonly JobId _jobId = JobId.New();
    protected readonly JobStepId _stepId = JobStepId.New();
    protected JobsStorage _storage;
    protected IJobs _jobs;
    protected JobKey _key;
    protected IntegrationJobStopSignals _signals;

    async Task Establish()
    {
        var fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        _key = new(JobsClusterFixture.Scope, string.Empty);
        _storage = fixture.Services.GetRequiredService<IJobsStorage>().GetFor(_key.Scope, _key.Namespace);
        _jobs = fixture.Services.GetRequiredService<IJobs>();
        _signals = IntegrationJobStopSignals.Register(_jobId);
        var type = fixture.Services.GetRequiredService<IJobTypes>().GetFor(typeof(IIntegrationJob)).AsT0;

        // These records are what survives a restart. Neither grain has activated or received Start in this silo.
        var job = new JobState
        {
            Id = _jobId,
            Type = type,
            Status = JobStatus.Running,
            Request = new IntegrationJobRequest(["held"]),
            Created = DateTimeOffset.UtcNow,
            Progress = new() { TotalSteps = 1 },
            StatusChanges = [new() { Status = JobStatus.StartingSteps }, new() { Status = JobStatus.Running }]
        };
        (await _storage.Jobs.Save(_jobId, job)).RethrowError();
        var step = new IntegrationJobStepState
        {
            Id = new(_jobId, _stepId),
            Type = typeof(IIntegrationJobStep),
            Item = "held",
            IsPrepared = true,
            Status = JobStepStatus.Scheduled,
            StatusChanges = [new() { Status = JobStepStatus.Scheduled }]
        };
        (await _storage.JobSteps.Save(_jobId, _stepId, step)).RethrowError();
    }

    void Destroy()
    {
        _signals.ContinueWork.TrySetResult();
        IntegrationJobStopSignals.Remove(_jobId);
    }
}
