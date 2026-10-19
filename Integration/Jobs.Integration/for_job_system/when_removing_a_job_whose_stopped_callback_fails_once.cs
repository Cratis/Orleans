// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Draining;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_removing_a_job_whose_stopped_callback_fails_once : Specification
{
    DrainingClusterFixture _fixture;
    Result<RemoveJobError> _removed;
    Catch<JobState, Storage.Jobs.JobError> _job;
    IEnumerable<JobStepState> _steps;

    void Establish()
    {
        _fixture = new();
        _fixture.Signals.FailFirstStop = true;
    }

    async Task Because()
    {
        var jobId = await _fixture.StartAndWaitForTheStepToPerform();
        _removed = await _fixture.GetJob(jobId).Remove().WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);

        // The first report of the stop finds the stopped callback failing; the step reports again, and that finishes it.
        await _fixture.Signals.JobStopped.Task.WaitAsync(DrainingClusterFixture.Deadline, TimeProvider.System);
        _steps = await WaitForTheStepToBeRemoved(jobId);
        _job = await _fixture.GetJobState(jobId);
    }

    [Fact] void should_accept_the_removal() => _removed.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_stopped_callback_again() => _fixture.Signals.StoppedCallbacks.ShouldEqual(2);
    [Fact] void should_clear_the_job() => _job.AsT1.ShouldEqual(Storage.Jobs.JobError.NotFound);
    [Fact] void should_remove_the_step() => _steps.ShouldBeEmpty();

    void Destroy() => _fixture.Dispose();

    async Task<IEnumerable<JobStepState>> WaitForTheStepToBeRemoved(JobId jobId)
    {
        var deadline = TimeProvider.System.GetUtcNow() + DrainingClusterFixture.Deadline;
        while (true)
        {
            var steps = await _fixture.GetStepStates(jobId);
            if (!steps.Any() || TimeProvider.System.GetUtcNow() > deadline)
            {
                return steps;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(50), TimeProvider.System);
        }
    }
}
