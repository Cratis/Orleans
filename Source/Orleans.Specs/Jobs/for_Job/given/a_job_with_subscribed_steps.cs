// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Utilities;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_with_subscribed_steps : a_running_job_with_steps
{
    protected IJobObserver _firstObserver;
    protected IJobObserver _secondObserver;
    protected ObserverManager<JobStepId, IJobObserver> _observers;

    protected IEnumerable<GrainTimerCreationOptions> NotificationTimerOptions => _silo.TimerRegistry.Mock.Invocations
        .SelectMany(invocation => invocation.Arguments).OfType<GrainTimerCreationOptions>()
        .Where(options => options.Period == Timeout.InfiniteTimeSpan);

    protected static Task NotificationFailed => Task.FromException(new TimeoutException("The step's silo could not be reached"));

    void Establish()
    {
        _firstObserver = Substitute.For<IJobObserver>();
        _secondObserver = Substitute.For<IJobObserver>();
        _observers = (ObserverManager<JobStepId, IJobObserver>)typeof(Job<SomeRequest, SomeJobState>)
            .GetField("_observers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!;
        _observers.Subscribe(_stepIds[0], _firstObserver);
        _observers.Subscribe(_stepIds[1], _secondObserver);
    }
}
