// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.when_getting_job_type;

public class with_a_missing_job : given.a_legacy_store
{
    Catch<JobType, JobError> _result;
    void Establish() => _original.GetJob(_id).Returns(Task.FromResult<Catch<JobState, JobError>>(JobError.NotFound));
    async Task Because() => _result = await _legacy.GetJobType(_id);

    [Fact] void should_preserve_not_found() => _result.AsT1.ShouldEqual(JobError.NotFound);
}
