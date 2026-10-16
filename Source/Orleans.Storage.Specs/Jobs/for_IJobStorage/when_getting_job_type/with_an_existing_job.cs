// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.when_getting_job_type;

public class with_an_existing_job : given.a_legacy_store
{
    Catch<JobType, JobError> _result;
    readonly JobType _type = new("legacy-job");
    void Establish() => _original.GetJob(_id).Returns(Task.FromResult<Catch<JobState, JobError>>(new JobState { Id = _id, Type = _type }));
    async Task Because() => _result = await _legacy.GetJobType(_id);

    [Fact] void should_fall_back_to_the_full_read() => _result.AsT0.ShouldEqual(_type);
}
