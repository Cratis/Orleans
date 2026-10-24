// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_while_a_drain_is_in_progress : given.a_controlled_backlog
{
    bool _drainWasStillRunning;
    int _initialResumeCount;
    Mock<INullJobWithSomeRequest> _newJob;

    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);

        // The newly interrupted job was not in the first discovery snapshot.
        _newJob = AddJob<INullJobWithSomeRequest>(JobId.New());
        _newJob.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        _storedJobs.RemoveAll(job => _jobIds.Contains(job.Id));
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        _drainWasStillRunning = !Rehydration.IsCompleted;
        _initialResumeCount = _resumed.Count;
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_return_without_waiting_for_the_in_progress_drain() => _drainWasStillRunning.ShouldBeTrue();
    [Fact] void should_not_multiply_the_concurrency_limit() => _initialResumeCount.ShouldEqual(2);
    [Fact] void should_make_a_fresh_pass_for_the_newly_interrupted_job() => _newJob.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_read_both_passes_without_finalized_stops() => _jobStorage.Received(2).GetJobsExcludingFinalizedStops(Arg.Any<JobStatus[]>());
}
