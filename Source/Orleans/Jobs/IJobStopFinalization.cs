// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Defines a job whose stop or removal can be finalized after the silo that began it went away.
/// </summary>
/// <remarks>
/// Every job implements this. The jobs manager calls it while rehydrating, for every job persisted as
/// <see cref="JobStatus.Stopped"/> or <see cref="JobStatus.Removing"/>.
/// </remarks>
public interface IJobStopFinalization : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Finalizes the stop or removal of the job when every one of its steps has stopped.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// <para>
    /// A job whose steps have all stopped, or no longer exist, runs its stopped hook; a job being removed then goes on
    /// to be removed. Steps that never recorded that they stopped are told to stop again, and the job finalizes once
    /// they report - exactly as it would have if its silo had stayed. Nothing is resumed: the stop stands.
    /// </para>
    /// <para>
    /// Calling it again does nothing once this activation has run the stopped hook, and nothing while the job still
    /// waits for its steps. A job that is not stopped or being removed, or whose drain is pending, is left as it is.
    /// The stopped hook may still run more than once for a job across activations, as its contract allows.
    /// </para>
    /// </remarks>
    Task FinalizeStop();
}
