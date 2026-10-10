// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// A prepared job step whose throttle acquisition can be observed and held open.
/// </summary>
public class a_job_step_with_a_gated_throttle : a_job_step_left_scheduled_by_a_dead_process
{
    protected GatedThrottle _throttle = new();

    /// <inheritdoc/>
    protected override IJobStepThrottle CreateThrottle() => _throttle;

    void Destroy()
    {
        _throttle.Slot.TrySetResult();
        _throttle.ReturnSlot.TrySetResult();
        _jobStep.Dispose();
    }
}
