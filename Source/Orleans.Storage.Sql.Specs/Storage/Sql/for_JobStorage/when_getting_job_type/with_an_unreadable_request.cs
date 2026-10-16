// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_job_type;

public class with_an_unreadable_request : given.jobs_in_storage
{
    Catch<JobType, JobError> _result;
    Catch<JobState, JobError> _fullRead;
    Task Establish() => WithJobs(_unknownJob);
    async Task Because()
    {
        _fullRead = await _storage.GetJob(_unknownJob.Id);
        _result = await _storage.GetJobType(_unknownJob.Id);
    }

    [Fact] void should_fail_to_read_the_request() => _fullRead.TryGetException(out _).ShouldBeTrue();
    [Fact] void should_still_return_the_stored_type() => _result.AsT0.ShouldEqual(new JobType(_unknownJob.Type));
}
