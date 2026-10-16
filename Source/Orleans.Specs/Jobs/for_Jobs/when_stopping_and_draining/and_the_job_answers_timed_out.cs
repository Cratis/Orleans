// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping_and_draining;

public class and_the_job_answers_timed_out : given.jobs
{
    readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
    DrainJobResult _result;

    void Establish() => ((IJobDrain)_job).StopAndDrain(_timeout).Returns(Task.FromResult(JobDrainStatus.TimedOut));

    async Task Because() => _result = await _jobs.StopAndDrain(_key, _id, _timeout);

    [Fact] void should_report_the_outcome_for_the_job() => _result.ShouldEqual(new DrainJobResult.TimedOut(_id));
    [Fact] void should_drain_the_job_with_the_timeout() => ((IJobDrain)_job).Received(1).StopAndDrain(_timeout);
}
