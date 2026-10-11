// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_stopping_before_the_step_performs : Specification
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);
    JobsClusterFixture _fixture;
    readonly JobId _jobId = JobId.New();
    IntegrationJobStopSignals _signals;
    JobState _jobState;
    JobStepState _stepState;

    void Establish()
    {
        _fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        _signals = IntegrationJobStopSignals.Register(_jobId);
    }

    async Task Because()
    {
        var manager = _fixture.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty);
        var started = await manager.Start<IIntegrationJob, IntegrationJobRequest>(_jobId, new IntegrationJobRequest(["held"]));
        started.IsSuccess.ShouldBeTrue();
        await _signals.WorkEntered.Task.WaitAsync(_deadline, TimeProvider.System);
        await manager.Stop(_jobId).WaitAsync(_deadline, TimeProvider.System);
        _signals.ContinueWork.TrySetResult();
        await _signals.WorkCompleted.Task.WaitAsync(_deadline, TimeProvider.System);
        _jobState = (await manager.GetAllJobs()).Single(job => job.Id == _jobId);
        var storage = _fixture.Services.GetRequiredService<IJobsStorage>().GetFor(JobsClusterFixture.Scope, string.Empty);
        _stepState = (await storage.JobSteps.GetForJob(_jobId)).AsT0.Single();
    }

    [Fact] void should_stop_the_parent_job() => _jobState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_report_one_stopped_step() => _jobState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_finish_stopping_the_job() => _jobState.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_run_the_jobs_on_stopped_callback() => _signals.Stopped.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_persist_the_step_as_stopped() => _stepState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_perform_the_step() => _signals.Performed.Task.IsCompleted.ShouldBeFalse();

    void Destroy()
    {
        _signals.ContinueWork.TrySetResult();
        IntegrationJobStopSignals.Remove(_jobId);
    }
}
