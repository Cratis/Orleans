// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages;

/// <summary>
/// Represents the progress of one stage of a job.
/// </summary>
public record JobStageProgress
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepStage"/> the progress is for.
    /// </summary>
    public JobStepStage Stage { get; set; } = JobStepStage.First;

    /// <summary>
    /// Gets or sets whether the steps in the stage have been started.
    /// </summary>
    public bool IsStarted { get; set; }

    /// <summary>
    /// Gets or sets the number of steps in the stage.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of steps in the stage that completed successfully.
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of steps in the stage that failed.
    /// </summary>
    public int FailedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of steps in the stage that never started because an earlier stage failed.
    /// </summary>
    public int UnreachableSteps { get; set; }

    /// <summary>
    /// Gets whether every step in the stage has reached an outcome.
    /// </summary>
    public bool IsCompleted => SuccessfulSteps + FailedSteps + UnreachableSteps == TotalSteps;

    /// <summary>
    /// Gets whether any step in the stage failed.
    /// </summary>
    public bool HasFailures => FailedSteps > 0;

    /// <summary>
    /// Gets the number of steps in the stage that have not reached an outcome yet.
    /// </summary>
    public int RemainingSteps => TotalSteps - SuccessfulSteps - FailedSteps - UnreachableSteps;
}
