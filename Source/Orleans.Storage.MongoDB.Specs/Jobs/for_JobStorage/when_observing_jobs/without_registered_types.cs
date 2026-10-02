// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

[Collection("MongoDBJobObservations")]
public class without_registered_types : given.an_observed_job_storage
{
    protected override Paging QueryPaging => new(0, 1, true);

    void Establish()
    {
        var types = Substitute.For<IJobTypes, IJobTypesCatalog>();
        ((IJobTypesCatalog)types).All.Returns([]);
        _storage = new JobStorage(_database, types);
    }

    async Task Because()
    {
        await ObserveInitialJobs();
        await InsertUnknownJob();
    }

    [Fact] void should_return_an_empty_page() => _initial.ShouldBeEmpty();
    [Fact] void should_count_no_jobs() => _initialTotal.ShouldEqual(0);
    [Fact] void should_keep_the_page_empty_after_an_unknown_insert() => _afterUnknown.ShouldBeEmpty();
    [Fact] void should_keep_the_total_at_zero_after_an_unknown_insert() => _afterUnknownTotal.ShouldEqual(0);
}
