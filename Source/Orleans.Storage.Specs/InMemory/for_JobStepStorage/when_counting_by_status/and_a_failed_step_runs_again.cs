// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStepStorage.when_counting_by_status;

public class and_a_failed_step_runs_again : Specification
{
    JobStepStorage _storage;
    JobId _jobId;
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    async Task Establish()
    {
        _storage = new();
        _jobId = JobId.New();
        var stepId = JobStepId.New();
        var state = new JobStepState { Id = new(_jobId, stepId), Type = typeof(IJobStep), Status = JobStepStatus.CompletedWithFailure };
        await _storage.Save(_jobId, stepId, state);
        state.Status = JobStepStatus.Scheduled;
        state.Attempt = 2;
        await _storage.Save(_jobId, stepId, state);
    }

    async Task Because() => _result = await _storage.CountByStatus(_jobId);

    [Fact] void should_count_the_step_as_scheduled() => _result.AsT0[JobStepStatus.Scheduled].ShouldEqual(1);
    [Fact] void should_no_longer_count_the_failure() => _result.AsT0.ContainsKey(JobStepStatus.CompletedWithFailure).ShouldBeFalse();
}
