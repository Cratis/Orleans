// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Monads;
using Moq;
using Orleans.Utilities;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_the_removal_cannot_be_persisted : given.a_running_job_with_steps
{
    Result<RemoveJobError> _result;
    IJobObserver _observer;

    protected override int? Window => 1;

    void Establish()
    {
        _job.FailWrites = true;
        _observer = Substitute.For<IJobObserver>();
        var observers = (ObserverManager<JobStepId, IJobObserver>)typeof(Job<given.SomeRequest, given.SomeJobState>)
            .GetField("_observers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!;
        observers.Subscribe(_stepIds[0], _observer);
    }

    async Task Because() => _result = await _job.Remove();

    [Fact] void should_refuse_the_removal() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_say_that_it_could_not_persist_the_removal() => (_result.TryGetError(out var refusal) ? refusal : RemoveJobError.Unknown).ShouldEqual(RemoveJobError.CouldNotPersistRemoval);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_remember_a_removal_that_never_happened() => _job.CurrentState.StatusChanges.Any(_ => _.Status == JobStatus.Removing).ShouldBeFalse();
    [Fact] void should_not_notify_steps_of_removal() => _observer.DidNotReceive().OnJobRemoved();
    [Fact] void should_not_account_for_unstarted_steps() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_retire_the_uncertain_activation() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
}
