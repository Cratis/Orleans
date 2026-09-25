// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_finding_what_stopped_the_job;

public class and_a_barrier_that_has_not_completed_has_failures : Specification
{
    JobStageProgress? _result;

    void Because() => _result = new[]
    {
        new JobStageProgress { Stage = 0, TotalSteps = 2, FailedSteps = 1 },
    }.StoppedBy(_ => JobStageFailureBehavior.StopJob);

    [Fact] void should_not_stop_the_job_while_the_stage_is_still_running() => _result.ShouldBeNull();
}
