// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_planning;

public class with_steps_in_several_stages : Specification
{
    IList<JobStageProgress> _result;

    void Because() => _result = JobStages.Plan([30, 0, 30, 10]);

    [Fact] void should_order_the_stages_by_their_value() => _result.Select(_ => _.Stage.Value).ShouldContainOnly(0, 10, 30);
    [Fact] void should_put_the_first_stage_first() => _result[0].Stage.ShouldEqual(JobStepStage.First);
    [Fact] void should_count_the_steps_in_each_stage() => _result.Select(_ => _.TotalSteps).ShouldContainOnly(1, 1, 2);
    [Fact] void should_not_mark_any_stage_as_started() => _result.Any(_ => _.IsStarted).ShouldBeFalse();
}
