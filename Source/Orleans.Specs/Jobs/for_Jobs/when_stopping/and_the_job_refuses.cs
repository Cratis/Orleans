// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping;

public class and_the_job_refuses : given.jobs
{
    StopJobResult _result;
    void Establish() => _job.Stop().Returns(Task.FromResult(Result.Failed<StopJobError>(StopJobError.JobIsNotRunning)));
    async Task Because() => _result = await _jobs.Stop(_key, _id);

    [Fact] void should_preserve_the_refusal() => _result.ShouldEqual(new StopJobResult.Refused(_id, StopJobError.JobIsNotRunning));
    [Fact] void should_not_report_stopped() => _result.IsStopped.ShouldBeFalse();
}
