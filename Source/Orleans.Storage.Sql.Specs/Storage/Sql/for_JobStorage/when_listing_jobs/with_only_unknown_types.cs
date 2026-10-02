// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_listing_jobs;

public class with_only_unknown_types : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        _unknownJob.StateJson = "not even JSON";
        return WithJobs(_unknownJob);
    }

    async Task Because() => _result = await _storage.GetJobs();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_no_jobs() => _result.AsT0.ShouldBeEmpty();
}
