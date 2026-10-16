// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Defines a job that can be stopped and drained: stopped so that none of its step work runs any more, with every
/// outcome persisted and the job's hooks run.
/// </summary>
/// <remarks>
/// Every job implements this. Use it only once every silo in the cluster runs a release that has it; a step hosted
/// by an older silo cannot be drained, so its job never answers <see cref="JobDrainStatus.Drained"/>.
/// </remarks>
public interface IJobDrain : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Stops the job and waits, bounded by <paramref name="timeout"/>, until its steps are drained.
    /// </summary>
    /// <param name="timeout">
    /// How long to wait. <see cref="TimeSpan.Zero"/> polls without waiting. The wait is capped below the Orleans
    /// response timeout, so calling again continues a drain that did not finish.
    /// </param>
    /// <returns>The <see cref="JobDrainStatus"/>.</returns>
    /// <remarks>
    /// <para>
    /// The first call persists the job as <see cref="JobStatus.Stopped"/> with the drain pending, then drains every step
    /// that can still run. <see cref="JobDrainStatus.Drained"/> means no step work of the job runs any more, each
    /// step's outcome and last checkpoint is in step storage, the job's step hooks and its stopped hook have run, and
    /// the job's state is persisted. A job that goes away before then completes the drain when it is activated again.
    /// </para>
    /// <para>
    /// Work that the job's hooks or step work start without awaiting is not covered. A successful step removes its
    /// record from step storage once its success is persisted.
    /// </para>
    /// <para>
    /// A drain completed after the job went away can run the step hook again for a step, or never: a successful step
    /// whose report was lost has no record left to drain, and a step that answered the earlier drain answers again
    /// without its outcome. Do not take a step's result from the hook alone; keep what you need in records of your own.
    /// </para>
    /// <para>
    /// The call holds the job's turn while it waits. Never await it with a timeout above zero from a grain turn that
    /// step work or the job's hooks call back into without interleaving - including the job's own grain; poll with
    /// <see cref="TimeSpan.Zero"/> instead.
    /// </para>
    /// </remarks>
    Task<JobDrainStatus> StopAndDrain(TimeSpan timeout);
}
