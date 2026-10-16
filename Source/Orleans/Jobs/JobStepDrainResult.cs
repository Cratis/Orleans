// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the outcome of draining a job step through <see cref="IJobStepDrain"/>.
/// </summary>
/// <remarks>
/// Part of the protocol between a job and its steps; see <see cref="IJobStepDrain"/>.
/// </remarks>
/// <param name="Drained">Whether no work of the step can run any more and its outcome is persisted.</param>
/// <param name="Status">The <see cref="JobStepStatus"/> the step ended in when drained, otherwise its current status.</param>
/// <param name="Outcome">The <see cref="JobStepResult"/> of the step's run, or null when it has none.</param>
[GenerateSerializer]
public sealed record JobStepDrainResult(
    [property: Id(0)] bool Drained,
    [property: Id(1)] JobStepStatus Status,
    [property: Id(2)] JobStepResult? Outcome);
