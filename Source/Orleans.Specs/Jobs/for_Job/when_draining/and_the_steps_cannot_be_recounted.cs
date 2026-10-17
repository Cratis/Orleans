// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

/// <summary>
/// Every step of a job activated with a pending drain answers the drain, but the job cannot read the steps back to
/// recount its progress from them.
/// </summary>
public class and_the_steps_cannot_be_recounted : given.a_job_activated_with_a_pending_drain
{
    JobDrainStatus _result;

    void Establish() =>
        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Is<JobStepStatus[]>(_ => _.Length == 0))
            .Returns(Task.FromResult<Catch<IImmutableList<JobStepState>>>(new InvalidOperationException("Step storage is unavailable")));

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_time_out() => _result.ShouldEqual(JobDrainStatus.TimedOut);
    [Fact] void should_keep_the_drain_pending() => _job.CurrentState.DrainPending.ShouldBeTrue();
    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
}
