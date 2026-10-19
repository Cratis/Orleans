// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStorage.when_round_tripping;

public class with_counted_stopped_steps : given.jobs_in_storage
{
    JobStepId _stepId;
    JobState _result;

    async Task Establish()
    {
        _stepId = JobStepId.New();
        (await _storage.Save(_id, new JobState { Id = _id, Type = _type, StoppedStepsCounted = [_stepId] })).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _result = (await _storage.Read<JobState>(_id)).AsT0;

    [Fact] void should_preserve_the_counted_steps() => _result.StoppedStepsCounted.ShouldContainOnly(_stepId);
}
