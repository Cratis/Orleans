// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans;

/// <summary>
/// Holds well known constants related to grain storage.
/// </summary>
public static class WellKnownGrainStorageProviders
{
    /// <summary>
    /// The name of the storage provider used for jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// The name of the storage provider used for job steps.
    /// </summary>
    public const string JobSteps = "job-steps";
}
