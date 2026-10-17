// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Represents the state of a job step.
/// </summary>
public class JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepId"/>.
    /// </summary>
    public JobStepIdentifier Id { get; set; } = JobStepIdentifier.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStepType"/> .
    /// </summary>
    public JobStepType Type { get; set; } = JobStepType.NotSet;

    /// <summary>
    /// Gets or sets the name of the job step.
    /// </summary>
    public JobStepName Name { get; set; } = JobStepName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStepStage"/> the step runs in.
    /// </summary>
    public JobStepStage Stage { get; set; } = JobStepStage.First;

    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job step.
    /// </summary>
    public IList<JobStepStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobStepProgress"/>.
    /// </summary>
    public JobStepProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the job step has been prepared.
    /// </summary>
    public bool IsPrepared { get; set; }

    /// <summary>
    /// Gets or sets the number of the attempt the step is on, starting at 1.
    /// </summary>
    /// <remarks>
    /// Optional in storage: a step written before attempts existed reads as attempt 1.
    /// </remarks>
    public int Attempt { get; set; } = 1;

    /// <summary>
    /// Gets or sets the <see cref="JobStepLease"/> held by the activation that runs the step, if leases are in use.
    /// </summary>
    /// <remarks>
    /// Optional in storage: a step written before leases existed, or while they are off, has none.
    /// </remarks>
    public JobStepLease? Lease { get; set; }
}
