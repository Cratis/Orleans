// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Stages;

/// <summary>
/// Request for <see cref="StagedIntegrationJob"/>.
/// </summary>
/// <param name="Stages">The items of each stage, in the order the stages run - one step runs per item.</param>
[GenerateSerializer]
public record StagedIntegrationJobRequest([Id(0)] IReadOnlyList<IReadOnlyList<string>> Stages) : IJobRequest;
