// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Interface for <see cref="IntegrationJobStep"/>.
/// </summary>
public interface IIntegrationJobStep : IJobStep<string, string, IntegrationJobStepState>;

/// <summary>
/// State held by <see cref="IntegrationJobStep"/>.
/// </summary>
public class IntegrationJobStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the item this step was created for.
    /// </summary>
    /// <remarks>
    /// The step only sees its state when it performs, not its original request, so the item is carried here.
    /// </remarks>
    public string Item { get; set; } = string.Empty;
}
