// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_the_stop_was_already_finalized : given.a_job_whose_silo_went_away_while_stopping
{
    void Establish() => _job.CurrentState.StopFinalized = true;

    Task Because() => _job.FinalizeStop();

    [Fact] void should_not_run_the_stopped_hook_again() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_not_write_the_job() => _job.WrittenStates.ShouldBeEmpty();
    [Fact] void should_let_the_job_deactivate() => Deactivations.ShouldEqual(1);
}
