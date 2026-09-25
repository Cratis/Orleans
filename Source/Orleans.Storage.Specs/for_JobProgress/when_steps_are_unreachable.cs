// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.for_JobProgress;

public class when_steps_are_unreachable : Specification
{
    JobProgress _progress;

    void Establish() => _progress = new() { TotalSteps = 3, FailedSteps = 1, UnreachableSteps = 2 };

    [Fact] void should_count_them_toward_completion() => _progress.IsCompleted.ShouldBeTrue();
    [Fact] void should_have_failures() => _progress.HasFailures.ShouldBeTrue();
}
