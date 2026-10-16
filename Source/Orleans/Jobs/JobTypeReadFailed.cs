// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The exception that is thrown when storage cannot determine a job's type.
/// </summary>
/// <param name="jobId">The job identity.</param>
/// <param name="error">The storage error.</param>
public class JobTypeReadFailed(JobId jobId, Storage.Jobs.JobError error)
    : Exception($"Could not read the type of job '{jobId}': {error}");
