// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_counting_by_status;

public class and_steps_have_mixed_statuses : job_steps_in_storage
{
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    async Task Establish()
    {
        await SaveStep(_jobId, JobStepStatus.Running);
        await SaveStep(_jobId, JobStepStatus.Running);
        await SaveStep(_jobId, JobStepStatus.Unknown);
        await SaveStep(_jobId, JobStepStatus.CompletedWithFailure);
        await SaveStep(_jobId, JobStepStatus.Failed);

        // Another job's steps are not counted.
        await SaveStep(JobId.New(), JobStepStatus.Running);
    }

    async Task Because() => _result = await _storage.CountByStatus(_jobId);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_running_steps() => _result.AsT0[JobStepStatus.Running].ShouldEqual(2);
    [Fact] void should_count_the_steps_not_started() => _result.AsT0[JobStepStatus.Unknown].ShouldEqual(1);
    [Fact] void should_count_the_steps_that_completed_with_failure() => _result.AsT0[JobStepStatus.CompletedWithFailure].ShouldEqual(1);
    [Fact] void should_count_the_unrecoverably_failed_steps() => _result.AsT0[JobStepStatus.Failed].ShouldEqual(1);
    [Fact] void should_not_include_statuses_without_steps() => _result.AsT0.ContainsKey(JobStepStatus.Stopped).ShouldBeFalse();

    Task SaveStep(JobId jobId, JobStepStatus status)
    {
        var jobStepId = JobStepId.New();
        return _storage.Save(jobId, jobStepId, AJobStepState(jobId, jobStepId, status));
    }
}
