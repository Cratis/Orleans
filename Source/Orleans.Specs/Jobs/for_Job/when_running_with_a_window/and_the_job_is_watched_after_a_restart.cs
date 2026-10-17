// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_the_job_is_watched_after_a_restart : for_Job.given.the_job
{
    List<Mock<for_Job.given.ISomeJobStep>> _steps = [];

    async Task Establish()
    {
        _job.Window = 2;
        for (var index = 0; index < 5; index++)
        {
            var id = JobStepId.New();
            var step = AddJobStep(id);
            step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Cratis.Monads.Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
            _steps.Add(step);
            StoredJobStep(id).Status = index < 2 ? JobStepStatus.Running : JobStepStatus.Unknown;
        }

        CountStepsInStorage((JobStepStatus.Running, 2), (JobStepStatus.Unknown, 3));
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 5;
        await _job.OnActivateAsync(CancellationToken.None);
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_widen_the_window_beyond_the_steps_that_are_running() => _steps.ShouldEachConformTo(step => step.Invocations.All(_ => _.Method.Name != nameof(IJobStep.Start)));
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
