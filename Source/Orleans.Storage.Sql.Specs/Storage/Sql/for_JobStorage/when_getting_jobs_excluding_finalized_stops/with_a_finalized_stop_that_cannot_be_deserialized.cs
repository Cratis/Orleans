// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_a_finalized_stop_that_cannot_be_deserialized : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        _firstJob.Status = JobStatus.Stopped;
        var finalized = ToEntity(_firstJob);
        finalized.StateJson = "{\"type\":\"custom-job-name\",\"request\":42,\"stopFinalized\":true}";
        return WithJobs(finalized, ToEntity(_secondJob));
    }

    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Running, JobStatus.Stopped);

    [Fact] void should_succeed_because_the_row_is_never_deserialized() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_only_the_other_job() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_secondJob.Id);
}
