// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStorage.when_round_tripping;

public class with_a_finalized_stop : given.jobs_in_storage
{
    JobState _result;

    async Task Establish() => (await _storage.Save(_id, new JobState { Id = _id, Type = _type, StopFinalized = true })).IsSuccess.ShouldBeTrue();

    async Task Because() => _result = (await _storage.Read<JobState>(_id)).AsT0;

    [Fact] void should_preserve_the_finalized_stop() => _result.StopFinalized.ShouldBeTrue();
}
