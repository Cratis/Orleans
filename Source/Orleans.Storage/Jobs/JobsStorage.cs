// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage;

/// <summary>
/// Represents the job and job step storage for a specific scope and namespace.
/// </summary>
/// <param name="Jobs">The <see cref="IJobStorage"/> for the scope.</param>
/// <param name="JobSteps">The <see cref="IJobStepStorage"/> for the scope.</param>
public record JobsStorage(IJobStorage Jobs, IJobStepStorage JobSteps);
