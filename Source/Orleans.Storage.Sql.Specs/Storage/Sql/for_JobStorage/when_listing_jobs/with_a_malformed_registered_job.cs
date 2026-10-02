// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_listing_jobs;

public class with_a_malformed_registered_job : given.jobs_in_storage
{
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        var malformed = ToEntity(_firstJob);
        malformed.StateJson = "{\"type\":\"custom-job-name\",\"request\":42}";
        return WithJobs(_unknownJob, malformed);
    }

    async Task Because() => _result = await _storage.GetJobs();

    [Fact] void should_report_the_deserialization_failure() => _result.IsSuccess.ShouldBeFalse();
}
