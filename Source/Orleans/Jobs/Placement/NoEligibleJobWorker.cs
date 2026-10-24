// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// The exception a host's job step placement director throws when no silo is eligible to host a job step worker.
/// </summary>
/// <remarks>
/// <para>
/// Throwing it is the only way a placement director can refuse to place a grain. It is deliberately not an
/// Orleans exception, so Orleans does not retry the placement; the exception reaches the caller, which is the
/// job controller, and the job system's existing failure handling applies. A job that fails while its steps are
/// being prepared ends failed with the message of this exception. A step that cannot be
/// reached while the job starts is counted as failed. A running job that is resumed or recovered keeps running
/// and retries the dispatch on its watchdog interval, but that retry is in memory only. The job system has no
/// durable wait for a worker.
/// </para>
/// <para>
/// A director must answer promptly. A director that blocks runs into the Orleans placement timeout instead.
/// </para>
/// </remarks>
/// <param name="grainId">The grain that could not be placed.</param>
/// <param name="reason">What is missing, for example the role the silos are expected to have.</param>
[GenerateSerializer]
[Alias("Cratis.Orleans.Jobs.Placement.NoEligibleJobWorker")]
public sealed class NoEligibleJobWorker(GrainId grainId, string reason)
    : Exception($"No silo is eligible to host job grain '{grainId}': {reason}");
