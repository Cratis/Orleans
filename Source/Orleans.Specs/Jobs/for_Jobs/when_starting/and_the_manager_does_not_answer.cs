// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class and_the_manager_does_not_answer : given.jobs
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<JobId, StartJobError> _result;
    void Establish() => _manager.GetJobs(Arg.Any<JobQuery>()).Returns(_answer.Task);
    async Task Because() => _result = await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request).WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_not_wait_for_the_manager() => _result.AsT0.ShouldEqual(_id);
    void Destroy() => _answer.TrySetResult([]);
}
