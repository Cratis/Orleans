// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_an_older_silo_dropped_its_recorded_outcome : given.a_job_being_removed_after_a_resume
{
    async Task Establish()
    {
        await ReportSuccessFromCurrentRun();
        var state = _job.WrittenStates[^1];

        // An older silo rewrites the state without the additive member. The deleted successful step cannot
        // reconstruct its identity from aggregate progress, so mixed clusters offer reduced protection.
        state.CompletedStepsCounted = [];
        _storedJobStepStates.Remove(StoredJobStep(_stepId));
        await ReactivateFrom(state);
    }

    async Task Because() => await ReportStopFromEarlierExecution();

    [Fact] void should_not_reconstruct_the_missing_record() => _job.CurrentState.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_have_only_legacy_stop_accounting() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_prevent_legacy_early_removal() => Clears.ShouldEqual(1);
}
