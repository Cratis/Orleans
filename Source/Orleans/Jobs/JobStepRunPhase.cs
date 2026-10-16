// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The phase of a <see cref="JobStepRun"/>, which decides who owns the run's outcome.
/// </summary>
internal enum JobStepRunPhase
{
    /// <summary>
    /// The run is being started and its work has not been launched.
    /// </summary>
    Starting = 0,

    /// <summary>
    /// The work is running and delivers its own outcome.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The run is being stopped; the stop delivers the outcome.
    /// </summary>
    Stopping = 2,

    /// <summary>
    /// The run is being stopped because its job is removed; the stop delivers the outcome.
    /// </summary>
    Removing = 3,

    /// <summary>
    /// The activation is deactivating; nothing is delivered and the step stays recoverable.
    /// </summary>
    Deactivating = 4
}
