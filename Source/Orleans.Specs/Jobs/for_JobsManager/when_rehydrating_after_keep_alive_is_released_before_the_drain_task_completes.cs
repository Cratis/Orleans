// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_after_keep_alive_is_released_before_the_drain_task_completes : given.a_controlled_backlog
{
    readonly Mock<ILogger<JobsManager>> _logger = new();
    Mock<INullJobWithSomeRequest> _newJob;
    Task _firstDrain;
    Task _secondPreparation;
    bool _keepAliveReleased;
    int _secondRehydrationClaimed;
    bool _firstDrainWasStillPending;

    async Task Establish()
    {
        var scope = new Mock<IDisposable>();
        _logger.Setup(logger => logger.BeginScope(It.IsAny<It.IsAnyType>())).Returns(scope.Object);
        _silo.GrainRuntime.Mock
            .Setup(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), TimeSpan.Zero))
            .Callback(() => _keepAliveReleased = true);
        scope.Setup(scope => scope.Dispose()).Callback(() =>
        {
            // Claim the one interleaving before calling Rehydrate: the second drain can dispose its own scope
            // on another thread before Rehydrate has returned.
            if (!_keepAliveReleased || Interlocked.Exchange(ref _secondRehydrationClaimed, 1) == 1)
            {
                return;
            }

            // The drain disposes its scope after its finally, but before its Task can complete.
            // Use that boundary only to arrange the interleaving, not to assert logging behavior.
            _firstDrainWasStillPending = !_firstDrain.IsCompleted;
            _keepAliveReleased = false;
            _secondPreparation = _manager.Rehydrate();
        });
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        _firstDrain = Rehydration;
        _storedJobs.Clear();
        _newJob = AddJob<INullJobWithSomeRequest>(JobId.New());
        _newJob.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because()
    {
        CompleteResumes();
        await _firstDrain.WaitAsync(Deadline, TimeProvider.System);
        await _secondPreparation.WaitAsync(Deadline, TimeProvider.System);
        await WaitForRehydration();
    }

    [Fact] void should_reproduce_an_incomplete_task_after_keep_alive_release() => _firstDrainWasStillPending.ShouldBeTrue();
    [Fact] void should_discover_and_resume_new_interrupted_jobs() => _newJob.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_start_a_new_drain() => ReferenceEquals(Rehydration, _firstDrain).ShouldBeFalse();

    protected override ILogger<JobsManager> CreateLogger() => _logger.Object;
}
