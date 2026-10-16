// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Defines how a job drains one of its steps.
/// </summary>
/// <remarks>
/// This is the protocol between a job and its steps. It is public only because grain interfaces have to be; drain
/// a job through the job instead.
/// </remarks>
public interface IJobStepDrain : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Stops the step and waits until its outcome is claimed, then persists that outcome.
    /// </summary>
    /// <param name="generation">The job's current execution generation.</param>
    /// <param name="timeout">How long to wait for the outcome to be claimed. <see cref="TimeSpan.Zero"/> only looks.</param>
    /// <returns>The <see cref="JobStepDrainResult"/>.</returns>
    /// <remarks>
    /// Waiting only observes the stop: the work keeps its callbacks until the stop's own deadline,
    /// <see cref="JobsOptions.StepStopTimeout"/> from when the stop began. The step's outcome report to the job is
    /// never awaited, because the draining job cannot take it until the drain returns.
    /// </remarks>
    [AlwaysInterleave]
    Task<JobStepDrainResult> Drain(long generation, TimeSpan timeout);
}
