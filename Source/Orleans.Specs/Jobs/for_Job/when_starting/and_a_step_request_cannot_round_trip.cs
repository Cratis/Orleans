// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_a_step_request_cannot_round_trip : given.a_job_with_a_durable_plan
{
    Result<StartJobError> _start;

    void Establish() => _job.StepsToPrepare[0] = _job.StepsToPrepare[0] with { Request = new given.SomeNonRoundTrippableRequest("input") };

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_with_a_typed_reason()
    {
        _start.TryGetError(out var error).ShouldBeTrue();
        error.ShouldEqual(StartJobError.InvalidStepPlan);
    }
    [Fact] void should_not_prepare_any_step() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_not_dispatch() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_save_an_unrecoverable_plan() => _job.CurrentState.StepPlan.ShouldBeNull();
}
