// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Workers;
using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents a step in a job.
/// </summary>
public interface IJobStep : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Prepare the job step to run in <see cref="JobStepStage.First"/>.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<PrepareJobStepError>> Prepare(object request);

    /// <summary>
    /// Prepare the job step to run in a given stage.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <param name="stage">The <see cref="JobStepStage"/> the step runs in.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The stage is persisted with the step, so a job resuming after a restart knows which stage each of its steps
    /// belongs to without planning its steps again.
    /// </remarks>
    Task<Result<PrepareJobStepError>> Prepare(object request, JobStepStage stage);

    /// <summary>
    /// Start the job step.
    /// </summary>
    /// <param name="jobGrainId">The <see cref="GrainId"/> for the parent job.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Starting is idempotent. <see cref="StartJobStepError.AlreadyStarted"/> and <see cref="StartJobStepError.Completed"/>
    /// report that the work is already in flight or already done - they are outcomes of a repeated start, not failures,
    /// and the step still reports its own completion to the job.
    /// </remarks>
    Task<Result<StartJobStepError>> Start(GrainId jobGrainId);

    /// <summary>
    /// Stop the job step.
    /// </summary>
    /// <param name="removing">Whether job step is being removed.</param>
    /// <remarks>
    /// A stopped job step can be started again later given it has been prepared.
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> Stop(bool removing);

    /// <summary>
    /// Report a status change.
    /// </summary>
    /// <param name="status">The <see cref="JobStepStatus"/> to change to.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> ReportStatusChange(JobStepStatus status);

    /// <summary>
    /// Report the step has failed.
    /// </summary>
    /// <param name="error">The <see cref="PerformJobStepError"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> ReportFailure(PerformJobStepError error);
}

/// <summary>
/// Represents a step in a job.
/// </summary>
/// <typeparam name="TRequest">Type of the request for the job step.</typeparam>
/// <typeparam name="TResult">Type of the result for the job step.</typeparam>
/// <typeparam name="TState">Type of the state.</typeparam>
public interface IJobStep<in TRequest, TResult, TState> : IGrainWithBackgroundTask<TRequest, JobStepResult>, IJobStep
{
    /// <summary>
    /// Prepare the job step.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<PrepareJobStepError>> Prepare(TRequest request);

    /// <summary>
    /// Gets the <typeparamref name="TState"/>.
    /// </summary>
    /// <returns>The state.</returns>
    [AlwaysInterleave]
    Task<TState> GetState();
}
