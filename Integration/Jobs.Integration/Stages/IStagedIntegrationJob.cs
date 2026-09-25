// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Stages;

/// <summary>
/// Interface for <see cref="StagedIntegrationJob"/>.
/// </summary>
public interface IStagedIntegrationJob : IJob<StagedIntegrationJobRequest>;
