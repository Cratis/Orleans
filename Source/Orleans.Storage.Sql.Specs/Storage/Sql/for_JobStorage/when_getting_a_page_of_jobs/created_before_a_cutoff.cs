// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_a_page_of_jobs;

public class created_before_a_cutoff : given.jobs_in_storage
{
    readonly JobId _included = JobId.New();
    Catch<IImmutableList<JobState>> _result;

    Task Establish() => WithJobs(
        ToEntity(RegisteredJob(_included, JobStatus.PreparingJob, new DateTimeOffset(2024, 1, 1, 10, 30, 0, TimeSpan.FromHours(2)))),
        ToEntity(RegisteredJob(JobId.New(), JobStatus.PreparingJob, new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.FromHours(2)))),
        ToEntity(RegisteredJob(JobId.New(), JobStatus.PreparingJob, new DateTimeOffset(2024, 1, 1, 9, 30, 0, TimeSpan.Zero))),
        ToEntity(RegisteredJob(JobId.New(), JobStatus.Running, new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero))));

    async Task Because() => _result = await _storage.GetJobs(new JobQuery
    {
        Statuses = [JobStatus.PreparingJob, JobStatus.PreparingSteps],
        CreatedBefore = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero)
    });

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess, _result.IsSuccess ? null : _result.AsT1.ToString());
    [Fact] void should_only_include_matching_jobs_strictly_before_the_cutoff_instant() => _result.AsT0.Select(job => job.Id).ShouldEqual([_included]);
}
