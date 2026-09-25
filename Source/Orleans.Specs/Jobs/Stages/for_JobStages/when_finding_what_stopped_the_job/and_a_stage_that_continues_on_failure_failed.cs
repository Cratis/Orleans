// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_finding_what_stopped_the_job;

public class and_a_stage_that_continues_on_failure_failed : Specification
{
    JobStageProgress? _result;

    void Because() => _result = new[]
    {
        new JobStageProgress { Stage = 0, TotalSteps = 1, FailedSteps = 1 },
        new JobStageProgress { Stage = 1, TotalSteps = 1 },
    }.StoppedBy(stage => stage == JobStepStage.First ? JobStageFailureBehavior.ContinueWithNextStage : JobStageFailureBehavior.StopJob);

    [Fact] void should_not_find_anything() => _result.ShouldBeNull();
}
