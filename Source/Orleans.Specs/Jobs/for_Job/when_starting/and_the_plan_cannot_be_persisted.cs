// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_the_plan_cannot_be_persisted : given.a_job_with_a_durable_plan
{
    Result<StartJobError> _start;

    void Establish() => _job.RejectWrite = state => state.Status is JobStatus.PreparingSteps;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_report_failure() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_prepare() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_not_dispatch() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_call_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
}
