// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// The exception that is thrown when the cluster's default placement strategy is itself one of the job placement strategies.
/// </summary>
/// <remarks>
/// The default job step placement director places workers the way the cluster default does, so a cluster default that
/// is a job placement strategy would make it delegate to itself. Configure a different cluster default placement
/// strategy, or register a host director with the generic job step placement registration on <see cref="Hosting.JobPlacementSiloBuilderExtensions"/>.
/// </remarks>
/// <param name="strategy">The type of the cluster default placement strategy.</param>
public class JobStepPlacementCannotUseItselfAsDefault(Type strategy)
    : Exception($"The cluster default placement strategy '{strategy.Name}' is a job placement strategy, so the default job step placement director cannot delegate to it. Configure a different cluster default placement strategy or register a job step placement director of your own.");
