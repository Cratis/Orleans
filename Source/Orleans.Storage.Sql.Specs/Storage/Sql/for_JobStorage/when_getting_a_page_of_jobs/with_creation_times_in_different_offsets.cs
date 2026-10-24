// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_a_page_of_jobs;

public class with_creation_times_in_different_offsets : given.jobs_in_storage
{
    readonly JobId _first = JobId.New();
    readonly JobId _second = JobId.New();
    readonly JobId _third = JobId.New();
    readonly JobId _fourth = JobId.New();
    Catch<IImmutableList<JobState>> _result;

    Task Establish() => WithJobs(
        ToEntity(RegisteredJob(_first, JobStatus.Running, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(2)))),
        ToEntity(RegisteredJob(_second, JobStatus.Running, new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero))),
        ToEntity(RegisteredJob(_third, JobStatus.Running, new DateTimeOffset(2024, 1, 1, 9, 30, 0, TimeSpan.Zero))),
        ToEntity(RegisteredJob(_fourth, JobStatus.Running, new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))));

    async Task Because() => _result = await _storage.GetJobs(new JobQuery { Skip = 1, Take = 2 });

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess, _result.IsSuccess ? null : _result.AsT1.ToString());
    [Fact] void should_page_in_instant_order() => _result.AsT0.Select(job => job.Id).ShouldEqual([_second, _third]);
}
