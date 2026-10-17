// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The local outcome of stopping and draining a job. This result does not cross a grain boundary.
/// </summary>
public abstract record DrainJobResult
{
    DrainJobResult()
    {
    }

    /// <summary>
    /// The job is stopped and drained; see <see cref="JobDrainStatus.Drained"/>.
    /// </summary>
    /// <param name="JobId">The drained job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of drain outcomes.")]
    public sealed record Drained(JobId JobId) : DrainJobResult;

    /// <summary>
    /// The job is stopped, but not every step was drained in time; see <see cref="JobDrainStatus.TimedOut"/>.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of drain outcomes.")]
    public sealed record TimedOut(JobId JobId) : DrainJobResult;

    /// <summary>
    /// The job had already completed or failed; see <see cref="JobDrainStatus.Completed"/>.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of drain outcomes.")]
    public sealed record Completed(JobId JobId) : DrainJobResult;

    /// <summary>
    /// No job with this identity exists in the requested scope and namespace.
    /// </summary>
    /// <param name="JobId">The missing job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of drain outcomes.")]
    public sealed record NotFound(JobId JobId) : DrainJobResult;

    /// <summary>
    /// The stored job type is not available in this release. No job grain was resolved.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    /// <param name="JobType">The stored type.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of drain outcomes.")]
    public sealed record UnknownJobType(JobId JobId, JobType JobType) : DrainJobResult;
}
