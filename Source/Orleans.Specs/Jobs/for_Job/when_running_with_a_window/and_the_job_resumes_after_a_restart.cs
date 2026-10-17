// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_the_job_resumes_after_a_restart : for_Job.given.the_job
{
    List<JobStepId> _ids = [];
    List<Mock<for_Job.given.ISomeJobStep>> _steps = [];

    void Establish()
    {
        _job.Window = 2;
        for (var index = 0; index < 5; index++)
        {
            var id = JobStepId.New();
            _ids.Add(id);
            var step = AddJobStep(id);
            step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Cratis.Monads.Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
            _steps.Add(step);

            // The window was two steps wide when the silo went away: two were running, the rest still waiting in storage.
            StoredJobStep(id).Status = index < 2 ? JobStepStatus.Running : JobStepStatus.Unknown;
        }

        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.Progress.TotalSteps = 5;
    }

    Task Because() => _job.Resume();

    [Fact] void should_start_the_steps_that_were_running() => _steps.Take(2).ShouldEachConformTo(step => step.Invocations.Count(_ => _.Method.Name == nameof(IJobStep.Start)) == 1);
    [Fact] void should_leave_the_steps_that_were_waiting_to_the_window() => _steps.Skip(2).ShouldEachConformTo(step => step.Invocations.All(_ => _.Method.Name != nameof(IJobStep.Start)));
}
