// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_a_page_of_jobs;

public class with_the_same_creation_time : given.jobs_in_storage
{
    readonly JobId _first = new(new Guid("00000000-0000-0000-0000-000000000001"));
    readonly JobId _second = new(new Guid("00000000-0000-0000-0000-000000000002"));
    readonly JobId _third = new(new Guid("00000000-0000-0000-0000-000000000003"));
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        var created = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
        return WithJobs(
            ToEntity(RegisteredJob(_third, JobStatus.Running, created)),
            ToEntity(RegisteredJob(_first, JobStatus.Running, created)),
            ToEntity(RegisteredJob(_second, JobStatus.Running, created)));
    }

    async Task Because() => _result = await _storage.GetJobs(new JobQuery());

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess, _result.IsSuccess ? null : _result.AsT1.ToString());
    [Fact] void should_order_by_id_to_break_ties() => _result.AsT0.Select(job => job.Id).ShouldEqual([_first, _second, _third]);
}
