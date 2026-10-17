// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_resuming_a_job_before_its_step_has_stopped : Specification
{
    DrainingClusterFixture _fixture;
    Result<ResumeJobSuccess, ResumeJobError> _resumed;
    JobState _jobState;

    void Establish()
    {
        _fixture = new();
        _fixture.Signals.HoldAfterCancellation = true;
        _fixture.Signals.SucceedWhenEnteredAgain = true;
    }

    async Task Because()
    {
        var jobId = await _fixture.StartAndWaitForTheStepToPerform();
        var job = _fixture.GetJob(jobId);
        (await job.Stop().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _fixture.Signals.Cancelled.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // The step's stop is still pending: its work holds on after observing cancellation.
        _resumed = await job.Resume().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _fixture.Signals.ReleaseWork.TrySetResult();
        await _fixture.Signals.JobCompleted.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // Inspect queues behind the non-reentrant turn that completed the job.
        _jobState = await job.Inspect().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
    }

    [Fact] void should_resume_the_job() => _resumed.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_step_again() => _fixture.Signals.Entries.ShouldEqual(2);
    [Fact] void should_not_count_the_stop_of_the_earlier_run() => _jobState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_count_the_step_as_succeeded() => _jobState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_complete_the_job() => _jobState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    void Destroy()
    {
        _fixture.Signals.ReleaseWork.TrySetResult();
        _fixture.Dispose();
    }
}
