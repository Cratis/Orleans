// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class and_removing_again_finishes_it : given.a_job_whose_removal_could_not_be_finished
{
    void Establish() => _job.FailClears = false;

    Task Because() => _job.Remove();

    [Fact] void should_clear_the_job() => Clears.ShouldEqual(1);
    [Fact] void should_cancel_the_pending_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
