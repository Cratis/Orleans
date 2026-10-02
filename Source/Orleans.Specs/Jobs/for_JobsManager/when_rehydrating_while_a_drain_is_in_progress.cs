// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_while_a_drain_is_in_progress : given.a_controlled_backlog
{
    bool _drainWasStillRunning;
    int _initialResumeCount;

    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        _drainWasStillRunning = !_rehydrationFinished.Task.IsCompleted;
        _initialResumeCount = _resumed.Count;
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_return_without_waiting_for_the_in_progress_drain() => _drainWasStillRunning.ShouldBeTrue();
    [Fact] void should_not_multiply_the_concurrency_limit() => _initialResumeCount.ShouldEqual(2);
    [Fact] void should_resume_each_job_only_once() => _resumed.OrderBy(id => id.Value).SequenceEqual(_jobIds.OrderBy(id => id.Value)).ShouldBeTrue();
}
