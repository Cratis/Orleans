// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job with a number of steps that is started and running, whose steps answer that they are already started - they
/// report their outcomes, which is what the specifications drive.
/// </summary>
public class a_running_job_with_steps : the_job
{
    protected List<JobStepId> _stepIds = [];
    protected List<Mock<ISomeJobStep>> _steps = [];

    protected virtual int NumberOfSteps => 2;

    protected virtual int? Window => null;

    protected int StartsOf(int index) => _steps[index].Invocations.Count(_ => _.Method.Name == nameof(IJobStep.Start));

    protected int Started => _steps.Count(step => step.Invocations.Any(_ => _.Method.Name == nameof(IJobStep.Start)));

    protected Task Succeed(int index) => _job.OnStepSucceeded(_stepIds[index], JobStepResult.Succeeded());

    protected Task Fail(int index, int attempt = 1)
    {
        RequestContext.Set(JobStepAttempt.Key, attempt);
        return _job.OnStepFailed(_stepIds[index], JobStepResult.Failed("failed"));
    }

    protected static void RequestWasReportedAs(int attempt) => RequestContext.Set(JobStepAttempt.Key, attempt);

    async Task Establish()
    {
        _job.Window = Window;
        for (var index = 0; index < NumberOfSteps; index++)
        {
            var id = Guid.Parse($"6c6c6c6c-0000-0000-0000-{index:D12}");
            _stepIds.Add(id);
            var step = AddJobStep(id);
            step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
            step.Setup(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>())).ReturnsAsync(Result<StartJobStepError>.Success());
            _steps.Add(step);
        }
        await _job.Start(new());
        foreach (var id in _stepIds)
        {
            StoredJobStep(id).Status = JobStepStatus.Running;
        }
    }
}
