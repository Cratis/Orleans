// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_stopping_a_job_queued_for_rehydration : given.a_controlled_backlog
{
    JobState _stoppedState;
    bool _jobWasStillQueued;

    void Establish()
    {
        var discoveredState = _storedJobs.Single(job => job.Id == _jobIds[2]);
        _stoppedState = new JobState
        {
            Id = discoveredState.Id,
            Type = discoveredState.Type,
            Created = discoveredState.Created,
            Status = JobStatus.Stopped
        };
        _jobs[2].Setup(job => job.Stop()).Callback(() =>
        {
            // Replace storage's state rather than mutating the discovery snapshot.
            _storedJobs[_storedJobs.IndexOf(discoveredState)] = _stoppedState;
        }).ReturnsAsync(Result.Success<StopJobError>());
        _jobs[2].Setup(job => job.Resume()).Callback(() => _stoppedState.Status = JobStatus.Running)
            .ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        _jobWasStillQueued = !_jobs[2].Invocations.Any(invocation => invocation.Method.Name == nameof(IJob.Resume));
        await _manager.Stop(_jobIds[2]);
        _completions[1].SetResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        await _started[3].Task.WaitAsync(Deadline, TimeProvider.System);
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_stop_a_job_that_had_not_yet_been_dispatched() => _jobWasStillQueued.ShouldBeTrue();
    [Fact] void should_stop_the_job_once() => _jobs[2].Verify(job => job.Stop(), Times.Once);
    [Fact] void should_not_resume_the_stopped_job() => _jobs[2].Verify(job => job.Resume(), Times.Never);
    [Fact] void should_leave_the_job_stopped() => _stoppedState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_continue_resuming_the_rest_of_the_backlog() => _resumed.OrderBy(id => id.Value).SequenceEqual(_jobIds.Where(id => id != _jobIds[2]).OrderBy(id => id.Value)).ShouldBeTrue();
}
