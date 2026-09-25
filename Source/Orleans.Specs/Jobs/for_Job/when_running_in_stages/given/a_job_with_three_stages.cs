// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages.given;

/// <summary>
/// A job whose first stage has one step, whose second stage has two steps and whose third stage has one step - the
/// shape of a pipeline where a single barrier is followed by a fan-out and then a final step.
/// </summary>
public class a_job_with_three_stages : for_Job.given.the_job
{
    protected JobStepId _first;
    protected JobStepId _secondA;
    protected JobStepId _secondB;
    protected JobStepId _third;
    protected Mock<for_Job.given.ISomeJobStep> _firstStep;
    protected Mock<for_Job.given.ISomeJobStep> _secondStepA;
    protected Mock<for_Job.given.ISomeJobStep> _secondStepB;
    protected Mock<for_Job.given.ISomeJobStep> _thirdStep;

    protected static void ShouldHaveStarted(Mock<for_Job.given.ISomeJobStep> step) =>
        step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);

    protected static void ShouldNotHaveStarted(Mock<for_Job.given.ISomeJobStep> step) =>
        step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);

    protected Task Succeed(JobStepId step) => _job.OnStepSucceeded(step, JobStepResult.Succeeded());

    protected Task Fail(JobStepId step) => _job.OnStepFailed(step, JobStepResult.Failed("failed"));

    void Establish()
    {
        _first = Guid.Parse("5a5a5a5a-0000-0000-0000-000000000001");
        _secondA = Guid.Parse("5a5a5a5a-0000-0000-0000-000000000002");
        _secondB = Guid.Parse("5a5a5a5a-0000-0000-0000-000000000003");
        _third = Guid.Parse("5a5a5a5a-0000-0000-0000-000000000004");

        // Declared out of stage order on purpose: the stage decides when a step starts, not its place in the plan.
        _thirdStep = AddStartableJobStep(_third, 20);
        _secondStepA = AddStartableJobStep(_secondA, 10);
        _firstStep = AddStartableJobStep(_first, 0);
        _secondStepB = AddStartableJobStep(_secondB, 10);
    }

    Mock<for_Job.given.ISomeJobStep> AddStartableJobStep(JobStepId id, int stage)
    {
        // Already started is a successful start that the harness can take without subscribing to a probe - the step
        // still reports its own outcome, which is what these specs drive.
        var step = AddJobStep(id, stage);
        step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        return step;
    }
}
