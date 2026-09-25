// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_reconciling;

public class from_steps_in_several_stages : Specification
{
    IList<JobStageProgress> _result;

    void Because() => _result = JobStages.Reconcile(
    [
        new JobStepState { Stage = 0, Status = JobStepStatus.CompletedSuccessfully },
        new JobStepState { Stage = 0, Status = JobStepStatus.CompletedWithFailure },
        new JobStepState { Stage = 1, Status = JobStepStatus.Failed },
        new JobStepState { Stage = 1, Status = JobStepStatus.Running },
        new JobStepState { Stage = 2, Status = JobStepStatus.Unreachable },
        new JobStepState { Stage = 3, Status = JobStepStatus.Unknown },
    ]);

    [Fact] void should_count_the_successful_steps_in_a_stage() => _result[0].SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_count_a_step_that_completed_with_failure_as_failed() => _result[0].FailedSteps.ShouldEqual(1);
    [Fact] void should_count_a_step_that_failed_as_failed() => _result[1].FailedSteps.ShouldEqual(1);
    [Fact] void should_leave_a_running_step_outstanding() => _result[1].RemainingSteps.ShouldEqual(1);
    [Fact] void should_count_the_unreachable_steps_in_a_stage() => _result[2].UnreachableSteps.ShouldEqual(1);
    [Fact] void should_mark_a_stage_with_a_step_that_ran_as_started() => _result[1].IsStarted.ShouldBeTrue();
    [Fact] void should_not_mark_a_stage_whose_steps_never_ran_as_started() => _result[3].IsStarted.ShouldBeFalse();
}
