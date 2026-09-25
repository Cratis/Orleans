// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages.for_JobStages.when_finding_what_stopped_the_job;

public class and_a_barrier_failed : Specification
{
    JobStageProgress _barrier;
    JobStageProgress? _result;

    void Establish() => _barrier = new() { Stage = 10, TotalSteps = 2, SuccessfulSteps = 1, FailedSteps = 1 };

    void Because() => _result = new[]
    {
        new JobStageProgress { Stage = 0, TotalSteps = 1, SuccessfulSteps = 1 },
        _barrier,
        new JobStageProgress { Stage = 20, TotalSteps = 1 },
    }.StoppedBy(_ => JobStageFailureBehavior.StopJob);

    [Fact] void should_find_the_barrier() => _result.ShouldEqual(_barrier);
}
