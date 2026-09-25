// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages;

/// <summary>
/// Represents the stage a job step runs in.
/// </summary>
/// <remarks>
/// Stages order the steps of a job: every step in a stage reaches an outcome before any step in a later stage
/// starts, while the steps within a stage run in parallel. Stages are ordered by their value, so gaps are harmless -
/// numbering by tens leaves room to insert one later. A job that never assigns a stage has every step in
/// <see cref="First"/>, which is a single stage and the job system's default fan-out.
/// </remarks>
/// <param name="Value">The ordinal of the stage.</param>
public record JobStepStage(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The stage every step belongs to unless it says otherwise.
    /// </summary>
    public static readonly JobStepStage First = new(0);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="JobStepStage"/>.
    /// </summary>
    /// <param name="value">The ordinal of the stage.</param>
    public static implicit operator JobStepStage(int value) => new(value);
}
