// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_planning;

public class with_every_step_in_one_stage : Specification
{
    IList<JobStageProgress> _result;

    void Because() => _result = JobStages.Plan([JobStepStage.First, JobStepStage.First]);

    [Fact] void should_not_record_any_stages() => _result.ShouldBeEmpty();
}
