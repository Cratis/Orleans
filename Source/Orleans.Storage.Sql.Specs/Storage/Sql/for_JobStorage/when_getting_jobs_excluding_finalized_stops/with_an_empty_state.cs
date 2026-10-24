// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_an_empty_state : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        var empty = ToEntity(new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Stopped });
        empty.StateJson = string.Empty;
        return WithJobs(empty);
    }

    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Stopped);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_the_stop() => _result.AsT0.Count.ShouldEqual(1);
}
