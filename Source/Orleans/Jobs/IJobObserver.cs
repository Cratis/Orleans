// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Defines a system for observing job events.
/// </summary>
/// <remarks>
/// A job sends these notifications from inside its own turn and waits for each step to take them, while a step
/// reports how it ended back to the job through a request of its own that waits for the job. A step that was
/// reporting at the moment the job stopped would therefore leave each grain waiting for the other until the call
/// timed out. The notifications interleave with whatever the step is doing so that cannot happen: for a step that
/// is running, taking the notification is only cancelling its work.
/// </remarks>
public interface IJobObserver : IGrainObserver
{
    /// <summary>
    /// Called when the job has stopped.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AlwaysInterleave]
    Task OnJobStopped();

    /// <summary>
    /// Called when the job has been removed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AlwaysInterleave]
    Task OnJobRemoved();
}
