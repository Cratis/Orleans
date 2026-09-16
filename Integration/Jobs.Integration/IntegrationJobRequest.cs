// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Request for <see cref="IntegrationJob"/> - the same authoring shape the getting-started documentation shows.
/// </summary>
/// <param name="Items">One step runs per item.</param>
[GenerateSerializer]
public record IntegrationJobRequest([Id(0)] IReadOnlyList<string> Items) : IJobRequest;
