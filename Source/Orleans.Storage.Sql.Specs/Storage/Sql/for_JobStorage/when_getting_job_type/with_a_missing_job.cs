// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_job_type;

public class with_a_missing_job : given.jobs_in_storage
{
    Catch<JobType, JobError> _result;
    async Task Because() => _result = await _storage.GetJobType(JobId.New());

    [Fact] void should_report_not_found() => _result.AsT1.ShouldEqual(JobError.NotFound);
}
