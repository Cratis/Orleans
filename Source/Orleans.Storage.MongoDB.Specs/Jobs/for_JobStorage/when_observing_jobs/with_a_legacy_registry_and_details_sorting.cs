// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

[Collection("MongoDBJobObservations")]
public class with_a_legacy_registry_and_details_sorting : given.an_observed_job_storage
{
    protected override bool HasCatalog => false;
    protected override Paging QueryPaging => new(0, 1, true);
    protected override Sorting QuerySorting => new(nameof(JobState.Details), Arc.Queries.SortDirection.Ascending);

    protected override void ConfigureJobs()
    {
        _firstJob.Details = "Charlie";
        _secondJob.Details = "Bravo";
        _thirdJob.Details = "Alpha";
        _unknownJob["details"] = "Zulu";
        _unknownJob["request"] = new BsonInt32(42);
    }

    async Task Because()
    {
        await ObserveInitialJobs();
        await InsertRegisteredJob();
    }

    [Fact] void should_sort_by_details_before_selecting_the_initial_page() => _initial.Select(job => job.Id).ShouldContainOnly(_secondJob.Id);
    [Fact] void should_preserve_the_details_of_the_visible_job() => _initial.Single().Details.ShouldEqual(_secondJob.Details);
    [Fact] void should_allow_an_insert_with_earlier_details_to_enter_the_page() => _afterKnown.Select(job => job.Id).ShouldContainOnly(_thirdJob.Id);
    [Fact] void should_preserve_the_inserted_jobs_details() => _afterKnown.Single().Details.ShouldEqual(_thirdJob.Details);
    [Fact] void should_keep_counting_stored_rows_for_a_legacy_registry() => _initialTotal.ShouldEqual(3);
    [Fact] void should_count_the_inserted_job() => _afterKnownTotal.ShouldEqual(4);
}
