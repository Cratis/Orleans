// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Utilities;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job whose earlier execution's stop was counted, but clearing failed and its current run cannot be told to remove.
/// </summary>
public class a_job_being_removed_whose_current_run_cannot_be_told : a_job_being_removed_after_a_resume
{
    protected IEnumerable<GrainTimerCreationOptions> TimerOptions => _silo.TimerRegistry.Mock.Invocations
        .SelectMany(invocation => invocation.Arguments).OfType<GrainTimerCreationOptions>();

    protected int RemovalRetriesArmed => TimerOptions.Count(options => options.KeepAlive);

    protected bool CurrentRunIsSubscribed => ((ObserverManager<JobStepId, IJobObserver>)typeof(Job<SomeRequest, SomeJobState>)
        .GetField("_observers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!).Observers.ContainsKey(_stepId);

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
        _currentRun.OnJobRemoved().Returns(Task.FromException(new InvalidOperationException()));
        await ReportStopFromEarlierExecution();
    }
}
