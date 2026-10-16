// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// What a step write carried to storage.
/// </summary>
/// <param name="Status">The persisted status.</param>
/// <param name="Message">The persisted progress message.</param>
public record PersistedStep(JobStepStatus Status, string Message);
