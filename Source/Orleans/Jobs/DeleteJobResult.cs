// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The local outcome of deleting a job. This result does not cross a grain boundary.
/// </summary>
public abstract record DeleteJobResult
{
    DeleteJobResult()
    {
    }

    /// <summary>
    /// Gets whether the job accepted removal.
    /// </summary>
    public bool IsDeleted => this is Deleted;

    /// <summary>
    /// The job accepted removal, including stopping and removing its steps.
    /// </summary>
    /// <param name="JobId">The removed job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of delete outcomes.")]
    public sealed record Deleted(JobId JobId) : DeleteJobResult;

    /// <summary>
    /// No job with this identity exists in the requested scope and namespace.
    /// </summary>
    /// <param name="JobId">The missing job.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of delete outcomes.")]
    public sealed record NotFound(JobId JobId) : DeleteJobResult;

    /// <summary>
    /// The stored job type is not available in this release. No job grain was resolved.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    /// <param name="JobType">The stored type.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of delete outcomes.")]
    public sealed record UnknownJobType(JobId JobId, JobType JobType) : DeleteJobResult;

    /// <summary>
    /// The job refused removal.
    /// </summary>
    /// <param name="JobId">The job identity.</param>
    /// <param name="Error">The job's refusal.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nested sealed records form the closed set of delete outcomes.")]
    public sealed record Refused(JobId JobId, RemoveJobError Error) : DeleteJobResult;
}
