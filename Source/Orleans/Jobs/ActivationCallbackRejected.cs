// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The reason a job step refused to run a callback on its activation.
/// </summary>
public enum ActivationCallbackRejected
{
    /// <summary>
    /// The callback was not requested from the background work of a job step run.
    /// </summary>
    NoRun = 0,

    /// <summary>
    /// The callback came from a run that is no longer the activation's current run.
    /// </summary>
    StaleRun = 1,

    /// <summary>
    /// The run no longer admits callbacks: it has been stopped or deactivated, or its stop timed out.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// The caller cancelled the callback while it was still waiting to run. A cancelled callback never runs.
    /// </summary>
    Cancelled = 3
}
