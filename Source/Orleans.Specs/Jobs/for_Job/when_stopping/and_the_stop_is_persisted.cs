// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_stopping;

public class and_the_stop_is_persisted : given.a_running_job_with_steps
{
    Task Because() => _job.Stop();

    [Fact] void should_make_the_stop_durable() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_stop_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
