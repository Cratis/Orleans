// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_planning_was_interrupted_without_a_saved_plan : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.PlanSteps = () => Task.FromException<IImmutableList<JobStepDetails>>(new InvalidOperationException("The silo stopped planning"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingJob);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_fail_with_a_typed_reason() => _result.AsT1.AsT0.ShouldEqual(CannotResumeJobError.MissingStepPlan);
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_dispatch() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_call_the_new_hook() => _job.DispatchContexts.ShouldBeEmpty();
}
