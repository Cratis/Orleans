// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents a <see cref="IJobStep"/> and the request associated with it.
/// </summary>
/// <param name="Type">Type of job step to create.</param>
/// <param name="Id">The unique identifier of the job step.</param>
/// <param name="Key">The key extension for the job step.</param>
/// <param name="Request">The request associated with the job step.</param>
/// <param name="ResultType">The result type for the job step.</param>
public record JobStepDetails(
    Type Type,
    JobStepId Id,
    JobStepKey Key,
    object Request,
    Type ResultType)
{
    /// <summary>
    /// Gets the <see cref="JobStepStage"/> the step runs in.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="JobStepStage.First"/>. A job whose steps all share one stage runs them all in parallel,
    /// which is the default and the recommended shape; give steps different stages only when a later step must not
    /// start until an earlier one has run.
    /// </remarks>
    public JobStepStage Stage { get; init; } = JobStepStage.First;
}
