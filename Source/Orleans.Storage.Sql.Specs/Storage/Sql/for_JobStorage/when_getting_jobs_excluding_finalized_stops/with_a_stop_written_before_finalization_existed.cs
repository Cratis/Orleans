// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_a_stop_written_before_finalization_existed : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        _firstJob.Status = JobStatus.Stopped;
        var legacy = ToEntity(_firstJob);
        var state = System.Text.Json.Nodes.JsonNode.Parse(legacy.StateJson)!.AsObject();
        state.Remove("stopFinalized");
        legacy.StateJson = state.ToJsonString();
        return WithJobs(legacy);
    }

    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Stopped);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_the_stop() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
}
