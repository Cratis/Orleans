// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

[Collection("MongoDBJobObservations")]
public class with_an_unknown_type_and_paging : given.an_observed_job_storage
{
    protected override Paging QueryPaging => new(0, 1, true);
    protected override Sorting QuerySorting => new(nameof(JobState.Created), Arc.Queries.SortDirection.Ascending);

    async Task Because()
    {
        await ObserveInitialJobs();
        await InsertUnknownJob();
        await InsertRegisteredJob();
    }

    [Fact] void should_fill_the_page_despite_an_earlier_unknown_job() => _initial.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_count_only_registered_jobs_outside_and_inside_the_page() => _initialTotal.ShouldEqual(2);
    [Fact] void should_not_evict_the_visible_job_after_an_unknown_insert() => _afterUnknown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_not_count_an_unknown_insert() => _afterUnknownTotal.ShouldEqual(2);
    [Fact] void should_allow_an_earlier_registered_insert_to_enter_the_page() => _afterKnown.Select(job => job.Id).ShouldContainOnly(_thirdJob.Id);
    [Fact] void should_count_the_registered_insert() => _afterKnownTotal.ShouldEqual(3);
    [Fact] void should_apply_paging_in_the_database_query() => _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<JobState>>(),
        Arg.Is<FindOptions<JobState, JobState>>(options => options.Skip == 0 && options.Limit == 1 && options.Sort != null),
        Arg.Any<CancellationToken>());
}
