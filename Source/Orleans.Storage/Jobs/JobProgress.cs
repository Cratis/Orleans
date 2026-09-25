// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
public record JobProgress
{
    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the completed number of steps.
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Gets or sets the failed number of steps.
    /// </summary>
    public int FailedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of stopped steps.
    /// </summary>
    public int StoppedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of steps that never started because a stage before them failed.
    /// </summary>
    /// <remarks>
    /// These count toward completion. A step behind a failed stage will never report an outcome, so without this
    /// the job could never account for every step and would sit in running forever.
    /// </remarks>
    public int UnreachableSteps { get; set; }

    /// <summary>
    /// Gets or sets the progress of each stage, in the order the stages run.
    /// </summary>
    /// <remarks>
    /// Empty for a job whose steps all run in <see cref="JobStepStage.First"/> - a single stage has nothing to order.
    /// </remarks>
    public IList<JobStageProgress> Stages { get; set; } = [];

    /// <summary>
    /// Gets whether the job is completed and there are no stopped steps.
    /// </summary>
    public bool IsCompleted => SuccessfulSteps + FailedSteps + UnreachableSteps == TotalSteps;

    /// <summary>
    /// Gets whether the job is completed where it also can have stopped steps.
    /// </summary>
    /// <remarks>
    /// Steps in a stage that has not started yet are never running, so they will never report being stopped - they
    /// count as stopped as they stand.
    /// </remarks>
    public bool IsStopped => SuccessfulSteps + FailedSteps + UnreachableSteps + StoppedSteps + StepsInStagesNotStarted == TotalSteps;

    /// <summary>
    /// Gets whether any step failed or never started.
    /// </summary>
    public bool HasFailures => FailedSteps > 0 || UnreachableSteps > 0;

    /// <summary>
    /// Gets or sets the current <see cref="JobProgressMessage"/> associated with the progress.
    /// </summary>
    public JobProgressMessage Message { get; set; } = JobProgressMessage.None;

    /// <summary>
    /// Gets the number of steps in stages that have not been started.
    /// </summary>
    int StepsInStagesNotStarted => Stages.Where(_ => !_.IsStarted).Sum(_ => _.RemainingSteps);
}
