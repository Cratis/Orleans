// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class and_the_manager_is_unavailable : given.jobs
{
    Result<JobId, StartJobError> _result;
    void Establish() => _manager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromException<System.Collections.Immutable.IImmutableList<JobState>>(new Exception("manager unavailable")));
    async Task Because() => _result = await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request);

    [Fact] void should_still_start_the_job() => _result.AsT0.ShouldEqual(_id);
}
