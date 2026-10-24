// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// The placement strategy of job controllers, the grains deriving from <see cref="Job{TRequest, TJobState}"/>.
/// </summary>
/// <remarks>
/// A host opts in with <see cref="Hosting.JobPlacementSiloBuilderExtensions"/>, with or without a host director. Without a host
/// director the default one places controllers exactly as they are placed without the opt-in.
/// </remarks>
[GenerateSerializer]
public sealed class JobsPlacementStrategy : PlacementStrategy;
