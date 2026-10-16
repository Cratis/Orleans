// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_reconciling;

/// <summary>
/// Every step of the first stage persisted its success, which removed its record, so only the second stage has
/// steps left in storage.
/// </summary>
public class with_a_planned_stage_that_left_no_steps : Specification
{
    IList<JobStageProgress> _result;

    void Because() => _result = JobStages.Reconcile(
        [new JobStepState { Stage = 1, Status = JobStepStatus.Unknown }],
        [new JobStageProgress { Stage = 0, TotalSteps = 2 }, new JobStageProgress { Stage = 1, TotalSteps = 1 }]);

    [Fact] void should_keep_both_stages() => _result.Count.ShouldEqual(2);
    [Fact] void should_keep_the_stage_without_steps_first() => _result[0].Stage.ShouldEqual((JobStepStage)0);
    [Fact] void should_have_completed_the_stage_without_steps() => _result[0].IsCompleted.ShouldBeTrue();
    [Fact] void should_have_the_stage_with_steps_next() => _result.Current().ShouldEqual(_result[1]);
    [Fact] void should_not_have_started_the_stage_with_steps() => _result[1].IsStarted.ShouldBeFalse();
}
