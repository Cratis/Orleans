// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.for_JobProgress;

public class when_a_stage_has_not_started : Specification
{
    JobProgress _progress;

    void Establish() => _progress = new()
    {
        TotalSteps = 3,
        SuccessfulSteps = 1,
        StoppedSteps = 1,
        Stages =
        [
            new() { Stage = 0, IsStarted = true, TotalSteps = 2, SuccessfulSteps = 1 },
            new() { Stage = 1, TotalSteps = 1 },
        ]
    };

    [Fact] void should_count_its_steps_as_stopped() => _progress.IsStopped.ShouldBeTrue();
    [Fact] void should_not_be_completed() => _progress.IsCompleted.ShouldBeFalse();
}
