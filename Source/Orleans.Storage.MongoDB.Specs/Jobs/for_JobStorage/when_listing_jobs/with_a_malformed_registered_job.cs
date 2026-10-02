// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_listing_jobs;

public class with_a_malformed_registered_job : given.a_job_storage
{
    Catch<IImmutableList<JobState>> _result;

    void Establish()
    {
        var malformed = _firstJob.ToBsonDocument();
        malformed["request"] = 42;
        WithDocuments([_unknownJob, malformed]);
    }

    async Task Because() => _result = await _storage.GetJobs();

    [Fact] void should_report_the_deserialization_failure() => _result.IsSuccess.ShouldBeFalse();
}
