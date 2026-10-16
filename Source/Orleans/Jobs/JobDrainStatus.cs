// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents how far a job has drained; see <see cref="IJobDrain.StopAndDrain"/>.
/// </summary>
public enum JobDrainStatus
{
    /// <summary>
    /// The job is stopped and drained: none of its step work runs any more and everything is persisted.
    /// </summary>
    Drained = 0,

    /// <summary>
    /// The job is stopped, but not every step was drained in time. Call again to continue the drain.
    /// </summary>
    TimedOut = 1,

    /// <summary>
    /// The job had already completed, or failed, and none of its step work runs any more.
    /// </summary>
    Completed = 2
}
