// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_Jobs;

[Collection(JobsClusterCollection.Name)]
public class when_starting_a_job : Specification
{
    JobsClusterFixture _fixture;
    readonly JobId _id = JobId.New();
    readonly JobKey _key = new(JobsClusterFixture.Scope, "tenant-a");
    IntegrationJobStopSignals _signals;
    JobState _state;
    Result<JobId, StartJobError> _result;

    void Establish()
    {
        _fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        _signals = IntegrationJobStopSignals.Register(_id);
        _signals.ContinueWork.TrySetResult();
    }

    async Task Because()
    {
        var jobs = _fixture.Services.GetRequiredService<IJobs>();
        _result = await jobs.Start<IIntegrationJob, IntegrationJobRequest>(_key, _id, new(["first", "second", "third"]));
        await _signals.Completed.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Queue behind completion on the job grain, including the durable status write.
        var job = _fixture.GrainFactory.GetGrain<IIntegrationJob>(_id, _key);
        (await job.Resume()).AsT0.ShouldEqual(ResumeJobSuccess.JobIsCompleted);
        _state = (await _fixture.Services.GetRequiredService<IJobsStorage>().GetFor(_key.Scope, _key.Namespace).Jobs.GetJob(_id)).AsT0;
    }

    [Fact] void should_return_the_started_identity() => _result.AsT0.ShouldEqual(_id);
    [Fact] void should_complete_successfully() => _state.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_perform_every_step() => _state.Progress.SuccessfulSteps.ShouldEqual(3);

    void Destroy() => IntegrationJobStopSignals.Remove(_id);
}
