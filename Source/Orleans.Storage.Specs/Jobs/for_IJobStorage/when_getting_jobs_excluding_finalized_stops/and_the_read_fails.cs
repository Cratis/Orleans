// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.when_getting_jobs_excluding_finalized_stops;

public class and_the_read_fails : given.a_legacy_store
{
    readonly Exception _error = new("storage unavailable");
    Catch<IImmutableList<JobState>> _result;

    void Establish() => _original.GetJobs(Arg.Any<JobStatus[]>()).Returns(Task.FromResult<Catch<IImmutableList<JobState>>>(_error));

    async Task Because() => _result = await _legacy.GetJobsExcludingFinalizedStops(JobStatus.Stopped);

    [Fact] void should_preserve_the_exception() => _result.AsT1.ShouldBeSame(_error);
}
