// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// Defines a step that keeps working until it is cancelled.
/// </summary>
public interface IDrainingStep : IJobStep<DrainingRequest, string, JobStepState>;
