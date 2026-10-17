// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Defines what an owner of jobs asks before an execution of one of them begins or begins again.
/// </summary>
/// <remarks>
/// <para>
/// Register an implementation with <see cref="Hosting.CratisOrleansSiloBuilderExtensions.AddJobAdmission{TAdmission}"/>
/// on every silo that can host the job types it concerns. A job asks every registered implementation, in registration
/// order, before it starts, before it resumes - including when a silo rehydrates it - and before it starts a later
/// stage after a step reached its outcome. The first refusal ends the question.
/// </para>
/// <para>
/// A refusal stops the job without running any of its step code. An implementation that throws, or that does not
/// answer within the bound the job applies, has not refused: the operation fails and is tried again the next time the
/// job is started, resumed or rehydrated.
/// </para>
/// <para>
/// The implementation runs inside the job's own non-reentrant turn. It can use storage, memory and calls to grains
/// that interleave or are stateless workers. It must never call the job it is asked about - <see cref="IJob"/>,
/// <see cref="IJobDrain"/>, <see cref="IJobs"/> or <see cref="IJobsManager"/> for it - nor its steps, nor a method that
/// does not interleave on a grain that may itself be waiting for this job's start or resume.
/// </para>
/// </remarks>
public interface IJobAdmission
{
    /// <summary>
    /// Decides whether an execution of a job may run.
    /// </summary>
    /// <param name="request">The <see cref="JobAdmissionRequest"/> describing the job.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that is cancelled when the job's bound for the answer has passed.</param>
    /// <returns>True if the job may run, false to refuse it.</returns>
    Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken);
}
