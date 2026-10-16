// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The local outcome of stopping a job. This result does not cross a grain boundary.
/// </summary>
public abstract record StopJobResult
{
    StopJobResult()
    {
    }

    /// <summary>
    /// Gets whether the job accepted the stop.
    /// </summary>
    public bool IsStopped => this is Stopped;

    /// <summary>
    /// The job accepted the stop.
    /// </summary>
    /// <param name="JobId">The stopped job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of stop outcomes.")]
    public sealed record Stopped(JobId JobId) : StopJobResult;

    /// <summary>
    /// No job with this identity exists in the requested scope and namespace.
    /// </summary>
    /// <param name="JobId">The missing job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of stop outcomes.")]
    public sealed record NotFound(JobId JobId) : StopJobResult;

    /// <summary>
    /// The stored job type is not available in this release. No job grain was resolved.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    /// <param name="JobType">The stored type.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of stop outcomes.")]
    public sealed record UnknownJobType(JobId JobId, JobType JobType) : StopJobResult;

    /// <summary>
    /// The job refused the stop.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    /// <param name="Error">The job's refusal.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of stop outcomes.")]
    public sealed record Refused(JobId JobId, StopJobError Error) : StopJobResult;
}
