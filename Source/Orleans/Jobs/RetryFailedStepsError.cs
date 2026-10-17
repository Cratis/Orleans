// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the reasons a job does not run its failed steps again.
/// </summary>
public enum RetryFailedStepsError
{
    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The job type does not allow its failed steps to be run again, or it has stages.
    /// </summary>
    NotSupported = 1,

    /// <summary>
    /// Only a job that completed with failures can run its failed steps again.
    /// </summary>
    JobIsNotCompletedWithFailures = 2,

    /// <summary>
    /// The change that reopens the job could not be made durable, so the job was left as it was.
    /// </summary>
    CouldNotPersist = 3,
}
