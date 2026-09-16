// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

/// <summary>
/// Scale spec: a job planning many steps fans them out to step grains and completes with every step counted -
/// the shape a real replay or migration brings.
/// </summary>
public class with_fifty_steps : given.the_job
{
    const int StepCount = 50;
    Result<StartJobError> _result;

    void Establish()
    {
        for (var n = 0; n < StepCount; n++)
        {
            var jobStep = AddJobStep(Guid.Parse($"1a1a1a1a-0000-0000-0000-{n:x012}"));
            jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        }
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_start_the_job() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_plan_every_step() => _job.CurrentState.Progress.TotalSteps.ShouldEqual(StepCount);
    [Fact] void should_leave_the_job_running_until_steps_report_back() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
