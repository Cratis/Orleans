// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_a_backlog : given.a_controlled_backlog
{
    int _initialResumeCount;
    int _initialStorageReads;
    int _resumeCountAfterOneCompletion;
    bool _firstResumeStillPending;
    bool _drainWasStillRunning;

    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        _initialResumeCount = _resumed.Count;
        _initialStorageReads = _jobStorage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IJobStorage.GetJob));
        _drainWasStillRunning = !_rehydrationFinished.Task.IsCompleted;

        _completions[1].SetResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        await _started[2].Task.WaitAsync(Deadline, TimeProvider.System);
        _resumeCountAfterOneCompletion = _resumed.Count;
        _firstResumeStillPending = !_completions[0].Task.IsCompleted;

        // Release one slot at a time: assert dispatch ordering, not the thread pool's ordering of
        // simultaneously released semaphore waiters (TestKit has no activation scheduler).
        for (var index = 2; index < _jobIds.Count; index++)
        {
            await _started[index].Task.WaitAsync(Deadline, TimeProvider.System);
            _completions[index].SetResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        }
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_only_dispatch_the_configured_number_initially() => _initialResumeCount.ShouldEqual(2);
    [Fact] void should_also_bound_the_job_state_reads() => _initialStorageReads.ShouldEqual(2);
    [Fact] void should_never_exceed_the_configured_concurrency() => _maximumConcurrentResumes.ShouldEqual(2);
    [Fact] void should_refill_a_slot_as_soon_as_it_is_available() => _resumeCountAfterOneCompletion.ShouldEqual(3);
    [Fact] void should_not_wait_for_the_whole_batch_to_complete() => _firstResumeStillPending.ShouldBeTrue();
    [Fact] void should_return_before_the_backlog_has_drained() => _drainWasStillRunning.ShouldBeTrue();
    [Fact] void should_keep_the_activation_alive_until_the_drain_finishes() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), Timeout.InfiniteTimeSpan), Times.Once);
    [Fact] void should_release_the_activation_after_the_drain_finishes() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), TimeSpan.Zero), Times.Once);
    [Fact] void should_resume_every_job_once_in_oldest_first_order() => _resumed.SequenceEqual(_jobIds).ShouldBeTrue();
}
