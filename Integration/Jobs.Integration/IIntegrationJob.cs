// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Interface for <see cref="IntegrationJob"/>.
/// </summary>
public interface IIntegrationJob : IJob<IntegrationJobRequest>;
