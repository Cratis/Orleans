// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The type of error that occurred while performing an operation on <see cref="IJobStep"/>.
/// </summary>
public enum JobStepError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Failed to persist state.
    /// </summary>
    FailedToPersistState = 1,

    /// <summary>
    /// Failed to report change in job step to job.
    /// </summary>
    FailedToReportToJob = 2,

    /// <summary>
    /// The step did not settle within <see cref="JobsOptions.StepStopTimeout"/>. The step is not quiescent: its
    /// work may still be running and nothing has been recorded or reported for the stop yet. The step reports and
    /// records its outcome once, when the work settles.
    /// </summary>
    StopTimedOut = 3
}
