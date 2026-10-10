// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// A throttle whose acquisition and return of an acquired slot can be held independently.
/// </summary>
public class GatedThrottle : IJobStepThrottle
{
    /// <summary>
    /// Gets the signal that the step has begun waiting for a slot.
    /// </summary>
    public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the gate that grants a slot, honoring cancellation while waiting.
    /// </summary>
    public TaskCompletionSource Slot { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that a slot has been granted, before acquisition returns to the step.
    /// </summary>
    public TaskCompletionSource Acquired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the gate holding the return of an already acquired slot to reproduce cancellation racing acquisition.
    /// </summary>
    public TaskCompletionSource ReturnSlot { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the number of acquired slots the step has released.
    /// </summary>
    public int Released { get; private set; }

    /// <inheritdoc/>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        Waiting.TrySetResult();
        await Slot.Task.WaitAsync(cancellationToken);
        Acquired.TrySetResult();
        await ReturnSlot.Task;
    }

    /// <inheritdoc/>
    public void Release() => Released++;
}
