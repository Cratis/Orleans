// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStepStorage.when_counting_by_status;

public class and_steps_have_mixed_statuses : Specification
{
    JobStepStorage _storage;
    JobId _jobId;
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    async Task Establish()
    {
        _storage = new();
        _jobId = JobId.New();
        await Save(_jobId, JobStepStatus.Running);
        await Save(_jobId, JobStepStatus.Running);
        await Save(_jobId, JobStepStatus.Unknown);
        await Save(_jobId, JobStepStatus.CompletedWithFailure);
        await Save(_jobId, JobStepStatus.Failed);

        // Another job's steps are not counted.
        await Save(JobId.New(), JobStepStatus.Running);
    }

    async Task Because() => _result = await _storage.CountByStatus(_jobId);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_running_steps() => _result.AsT0[JobStepStatus.Running].ShouldEqual(2);
    [Fact] void should_count_the_steps_not_started() => _result.AsT0[JobStepStatus.Unknown].ShouldEqual(1);
    [Fact] void should_count_the_steps_in_the_failed_collection_too() => _result.AsT0[JobStepStatus.CompletedWithFailure].ShouldEqual(1);
    [Fact] void should_count_the_unrecoverably_failed_steps() => _result.AsT0[JobStepStatus.Failed].ShouldEqual(1);
    [Fact] void should_not_include_statuses_without_steps() => _result.AsT0.ContainsKey(JobStepStatus.Stopped).ShouldBeFalse();

    Task Save(JobId jobId, JobStepStatus status)
    {
        var stepId = JobStepId.New();
        return _storage.Save(jobId, stepId, new JobStepState { Id = new(jobId, stepId), Type = typeof(IJobStep), Status = status });
    }
}
