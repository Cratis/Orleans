// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStorage.when_round_tripping;

public class with_counted_completed_steps : given.jobs_in_storage
{
    JobStepId _first;
    JobStepId _second;
    JobState _result;

    async Task Establish()
    {
        _first = JobStepId.New();
        _second = JobStepId.New();
        (await _storage.Save(_id, new JobState { Id = _id, Type = _type, CompletedStepsCounted = [_first, _second] })).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _result = (await _storage.Read<JobState>(_id)).AsT0;

    [Fact] void should_preserve_the_counted_steps() => _result.CompletedStepsCounted.ShouldContainOnly(_first, _second);
}
