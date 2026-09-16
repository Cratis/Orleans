// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Holds the well known collection names used by the jobs storage.
/// </summary>
public static class WellKnownCollectionNames
{
    /// <summary>
    /// The name of the collection holding job state.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// The name of the collection holding job step state.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The name of the collection holding failed job steps.
    /// </summary>
    public const string FailedJobSteps = "failed-job-steps";
}
