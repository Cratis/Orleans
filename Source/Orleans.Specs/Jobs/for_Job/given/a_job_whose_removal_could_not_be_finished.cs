// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job being removed whose steps have all reported their stops, but clearing it failed.
/// </summary>
public class a_job_whose_removal_could_not_be_finished : a_job_being_removed_with_two_steps
{
    protected int Clears => _silo.StorageStats<SomeJob, SomeJobState>().Clears;

    protected IEnumerable<GrainTimerCreationOptions> TimerOptions => _silo.TimerRegistry.Mock.Invocations
        .SelectMany(invocation => invocation.Arguments).OfType<GrainTimerCreationOptions>();

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
        await _job.OnStepStopped(_stepId, Stopped);
    }
}
