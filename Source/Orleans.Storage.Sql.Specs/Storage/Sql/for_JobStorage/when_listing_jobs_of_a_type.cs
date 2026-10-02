// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage;

public class when_listing_jobs_of_a_type : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>, Storage.Jobs.JobError> _result;

    Task Establish() => WithJobs(_unknownJob, ToEntity(_firstJob), ToEntity(new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.CompletedSuccessfully }));
    async Task Because() => _result = await _storage.GetJobs<IJob, JobState>(JobStatus.Running);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_filter_by_type_and_status() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_deserialize_the_known_request() => _result.AsT0.Single().Request.ShouldEqual(_firstJob.Request);
}
