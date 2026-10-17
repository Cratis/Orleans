// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Represents the claim an activation holds on a running job step, kept alive by heartbeats.
/// </summary>
public class JobStepLease
{
    /// <summary>
    /// Gets or sets the address of the silo that hosts the activation running the step.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity of the activation that runs the step.
    /// </summary>
    public Guid ActivationId { get; set; }

    /// <summary>
    /// Gets or sets when the activation last proved it was alive.
    /// </summary>
    public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets or sets the attempt the lease was taken for.
    /// </summary>
    public int Attempt { get; set; } = 1;
}
