// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_parent_provider_uses_the_full_job_fallback : given.a_job_step_with_a_stored_parent
{
    IJobStorage _fullJobStorage;

    void Establish()
    {
        _fullJobStorage = Substitute.For<IJobStorage>();
        _fullJobStorage.GetJob(_jobId).Returns(Task.FromResult<Catch<JobState, Storage.Jobs.JobError>>(new JobState { Id = _jobId, Type = _parentType }));
        var jobsStorage = Substitute.For<IJobsStorage>();
        jobsStorage.GetFor(_parentKey.Scope, _parentKey.Namespace).Returns(new JobsStorage(new given.job_storage_without_metadata(_fullJobStorage), Substitute.For<IJobStepStorage>()));
        _silo.ServiceProvider.AddService(jobsStorage);
    }

    async Task Because()
    {
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_read_the_full_job_through_the_provider_fallback() => await _fullJobStorage.Received(1).GetJob(_jobId);
    [Fact] void should_report_to_the_resolved_parent() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] void should_persist_the_accepted_stop() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);
}
