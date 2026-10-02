// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_the_activation_is_invalidated_before_releasing_keep_alive : given.a_controlled_backlog
{
    Task _drain;

    void Establish() => _silo.GrainRuntime.Mock
        .Setup(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), TimeSpan.Zero))
        .Throws(new InvalidOperationException("Activation is no longer valid"));

    async Task Because()
    {
        await _manager.Rehydrate();
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        _drain = (Task)typeof(JobsManager).GetField("_rehydration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
        await _manager.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _deactivated = true;
        CompleteResumes();
    }

    [Fact] void should_observe_and_finish_the_drain_without_a_fault() => _drain.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_attempt_to_release_the_keep_alive_once() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), TimeSpan.Zero), Times.Once);
}
