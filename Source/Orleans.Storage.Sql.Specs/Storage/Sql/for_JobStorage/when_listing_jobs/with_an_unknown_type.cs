// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_listing_jobs;

public class with_an_unknown_type : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish() => WithJobs(_unknownJob, ToEntity(_firstJob), ToEntity(_secondJob), ToEntity(new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.CompletedSuccessfully }));
    async Task Because() => _result = await _storage.GetJobs(JobStatus.Running);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_both_registered_jobs_with_the_requested_status() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_deserialize_the_known_requests() => _result.AsT0.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request);
    [Fact] async Task should_leave_all_stored_jobs_untouched()
    {
        await using var context = CreateContext();
        (await context.Jobs.CountAsync()).ShouldEqual(4);
        (await context.Jobs.SingleAsync(job => job.Id == _unknownJob.Id)).StateJson.ShouldEqual(_unknownJob.StateJson);
    }
}
