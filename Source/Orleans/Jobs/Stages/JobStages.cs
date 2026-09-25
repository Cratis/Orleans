// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.Stages;

/// <summary>
/// Computes the stages of a job from its steps.
/// </summary>
public static class JobStages
{
    /// <summary>
    /// Plan the stages for the steps of a job.
    /// </summary>
    /// <param name="stages">The <see cref="JobStepStage"/> of every step in the job.</param>
    /// <returns>The <see cref="JobStageProgress"/> for each stage, in the order they run - empty when every step is in the same stage.</returns>
    /// <remarks>
    /// A single stage has nothing to order, so it is not recorded - the job runs exactly as a job that never used
    /// stages does.
    /// </remarks>
    public static IList<JobStageProgress> Plan(IEnumerable<JobStepStage> stages)
    {
        var planned = stages
            .GroupBy(_ => _.Value)
            .OrderBy(_ => _.Key)
            .Select(_ => new JobStageProgress { Stage = _.Key, TotalSteps = _.Count() })
            .ToList();
        return planned.Count > 1 ? planned : [];
    }

    /// <summary>
    /// Rebuild the progress of each stage from the persisted state of the job's steps.
    /// </summary>
    /// <param name="steps">The persisted <see cref="JobStepState"/> of every step in the job.</param>
    /// <returns>The <see cref="JobStageProgress"/> for each stage, in the order they run - empty when every step is in the same stage.</returns>
    /// <remarks>
    /// Stage progress is counted as outcomes are reported, so an outcome that never reached storage leaves a stage
    /// short of its total forever. The steps are the record of what happened, which is what this counts from.
    /// </remarks>
    public static IList<JobStageProgress> Reconcile(IEnumerable<JobStepState> steps)
    {
        var reconciled = steps
            .GroupBy(_ => _.Stage.Value)
            .OrderBy(_ => _.Key)
            .Select(_ => new JobStageProgress
            {
                Stage = _.Key,
                IsStarted = _.Any(step => step.Status is not JobStepStatus.Unknown),
                TotalSteps = _.Count(),
                SuccessfulSteps = _.Count(step => step.Status.IsSuccessful()),
                FailedSteps = _.Count(step => step.Status.IsFailed()),
                UnreachableSteps = _.Count(step => step.Status is JobStepStatus.Unreachable)
            })
            .ToList();
        return reconciled.Count > 1 ? reconciled : [];
    }

    /// <summary>
    /// Get the stage that is currently running - the first one that has not completed.
    /// </summary>
    /// <param name="stages">The stages of the job, in the order they run.</param>
    /// <returns>The current <see cref="JobStageProgress"/>, or null when every stage has completed.</returns>
    public static JobStageProgress? Current(this IEnumerable<JobStageProgress> stages) =>
        stages.FirstOrDefault(_ => !_.IsCompleted);

    /// <summary>
    /// Get the completed stage whose failures stop the job, if any.
    /// </summary>
    /// <param name="stages">The stages of the job, in the order they run.</param>
    /// <param name="behaviorFor">Resolves the <see cref="JobStageFailureBehavior"/> for a stage.</param>
    /// <returns>The <see cref="JobStageProgress"/> that stopped the job, or null when nothing has.</returns>
    public static JobStageProgress? StoppedBy(this IEnumerable<JobStageProgress> stages, Func<JobStepStage, JobStageFailureBehavior> behaviorFor) =>
        stages
            .TakeWhile(_ => _.IsCompleted)
            .FirstOrDefault(_ => _.HasFailures && behaviorFor(_.Stage) is JobStageFailureBehavior.StopJob);

    static bool IsSuccessful(this JobStepStatus status) => status is JobStepStatus.CompletedSuccessfully;

    static bool IsFailed(this JobStepStatus status) => status is JobStepStatus.CompletedWithFailure or JobStepStatus.Failed;
}
