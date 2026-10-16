// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// Defines a job with one step that keeps working until it is cancelled.
/// </summary>
public interface IDrainingJob : IJob<DrainingRequest>
{
    /// <summary>
    /// Gets the state of the job.
    /// </summary>
    /// <returns>The <see cref="JobState"/>.</returns>
    Task<JobState> Inspect();
}

/// <summary>
/// The request for a <see cref="IDrainingJob"/>.
/// </summary>
[GenerateSerializer]
public sealed record DrainingRequest : IJobRequest;
