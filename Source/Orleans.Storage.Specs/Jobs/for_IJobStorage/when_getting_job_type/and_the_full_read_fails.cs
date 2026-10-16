// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.when_getting_job_type;

public class and_the_full_read_fails : given.a_legacy_store
{
    Catch<JobType, JobError> _result;
    readonly Exception _error = new("unreadable request");
    void Establish() => _original.GetJob(_id).Returns(Task.FromResult<Catch<JobState, JobError>>(_error));
    async Task Because() => _result = await _legacy.GetJobType(_id);

    [Fact] void should_preserve_the_exception() => _result.AsT2.ShouldBeSame(_error);
}
