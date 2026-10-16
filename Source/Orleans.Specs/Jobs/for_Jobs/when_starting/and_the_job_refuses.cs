// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class and_the_job_refuses : given.jobs
{
    Result<JobId, StartJobError> _result;
    void Establish() => _job.Start(_request).Returns(Task.FromResult(Result.Failed<StartJobError>(StartJobError.AlreadyBeenPrepared)));
    async Task Because() => _result = await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request);

    [Fact] void should_preserve_the_refusal() => _result.AsT1.ShouldEqual(StartJobError.AlreadyBeenPrepared);
}
