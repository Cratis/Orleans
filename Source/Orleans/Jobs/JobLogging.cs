// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public static partial class JobLogMessages
{
    [LoggerMessage(LogLevel.Warning, "An error occurred while writing state")]
    public static partial void FailedWritingState(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Starting job")]
    public static partial void Starting(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Resuming job")]
    public static partial void Resuming(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Restarting steps stopped before the last resume")]
    public static partial void RestartingStepsStoppedBeforeLastResume(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job was left running with all of its steps already completed - finalizing it instead of resuming")]
    public static partial void FinalizingJobLeftRunningAfterAllStepsCompleted(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job was stopped with all of its steps already completed - finalizing it instead of leaving it stopped")]
    public static partial void FinalizingStoppedJobWithNothingLeftToRun(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Job left {Status} with all of its steps stopped - finalizing its stop")]
    public static partial void FinalizingStopAfterItsSiloWentAway(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Information, "Job is still waiting for {Count} steps to stop - telling them to stop again")]
    public static partial void StopIsWaitingForSteps(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Job failed recounting its progress from its job steps")]
    public static partial void FailedReconcilingProgressFromJobSteps(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Stopping job")]
    public static partial void Stopping(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Removing job")]
    public static partial void Removing(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Job changing status to {Status}")]
    public static partial void ChangingStatus(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Debug, "There are no prepared job steps. Completing job")]
    public static partial void NoJobStepsToStart(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Found {JobStepsCount} job steps to prepare and start")]
    public static partial void PreparingJobSteps(this ILogger<IJob> logger, int jobStepsCount);

    [LoggerMessage(LogLevel.Debug, "An error occurred while preparing and starting job step {JobStepId}")]
    public static partial void ErrorPreparingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "An error occurred while preparing and starting job steps")]
    public static partial void ErrorPreparingJobSteps(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Failed starting the job steps in the background with error {Error}")]
    public static partial void FailedStartingJobStepsInBackground(this ILogger<IJob> logger, StartJobError error);

    [LoggerMessage(LogLevel.Debug, "Not starting the job steps because the job is no longer preparing, it is {Status}")]
    public static partial void NotStartingJobStepsForJobThatIsNoLongerPreparing(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Debug, "Ignoring late outcome from step {JobStepId}; job no longer accepts it in status {Status}")]
    public static partial void IgnoringLateStepOutcome(this ILogger<IJob> logger, JobStepId jobStepId, JobStatus status);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} successfully completed")]
    public static partial void StepSuccessfullyCompleted(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} stopped")]
    public static partial void StepStopped(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} failed")]
    public static partial void StepFailed(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Step {JobStepId} is no longer tracked by the job, skipping unsubscribe")]
    public static partial void CompletedJobStepIsNoLongerTracked(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Preparing job steps for running")]
    public static partial void PrepareJobStepsForRunning(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed")]
    public static partial void Failed(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to get job steps")]
    public static partial void FailedToGetJobSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new total steps count")]
    public static partial void FailedToSetTotalSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new job status {Status}")]
    public static partial void FailedWritingStatusChange(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated successful steps {SuccessfulStepsCount}")]
    public static partial void FailedUpdatingSuccessfulSteps(this ILogger<IJob> logger, Exception ex, int successfulStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated failed steps {FailedStepsCount}")]
    public static partial void FailedUpdatingFailedSteps(this ILogger<IJob> logger, Exception ex, int failedStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed remove state for all job steps and the job itself")]
    public static partial void FailedToRemoveForJob(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed. Error: {JobError}")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} {JobType} failed unexpectedly on OnAllStepsCompleted")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception ex, JobId jobId, JobType jobType);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed while there are no job steps to run, but Job will still clear state. Error: {JobError}")]
    public static partial void FailedOnCompletedWhileNoJobSteps(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Debug, "Job is not in a running state. Status: {JobStatus}")]
    public static partial void JobIsNotRunning(this ILogger<IJob> logger, JobStatus jobStatus);

    [LoggerMessage(LogLevel.Warning, "Job failed handling completed job step {JobStepId}. Error: {JobError}")]
    public static partial void FailedHandlingCompletedJobStep(this ILogger<IJob> logger, JobStepId jobStepId, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed persisting state after handling job step completion for job step {JobStepId}")]
    public static partial void FailedUpdatingStateAfterHandlingJobStepCompletion(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed persisting state after handling completion")]
    public static partial void FailedUpdatingStateAfterHandlingCompletion(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Job failed preparing job step {JobStepId}. Error: {Error}")]
    public static partial void FailedPreparingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, PrepareJobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed starting job step {JobStepId}. Error: {Error}")]
    public static partial void FailedStartingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, StartJobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed starting job step {JobStepId}")]
    public static partial void FailedStartingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Job step {JobStepId} was already started. Outcome: {Outcome}")]
    public static partial void JobStepWasAlreadyStarted(this ILogger<IJob> logger, JobStepId jobStepId, StartJobStepError outcome);

    [LoggerMessage(LogLevel.Warning, "Job failed subscribing to job step {JobStepId}")]
    public static partial void FailedSubscribingToJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as failed. Error: {Error}")]
    public static partial void FailedRecordingJobStepAsFailed(this ILogger<IJob> logger, JobStepId jobStepId, JobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as failed")]
    public static partial void FailedRecordingJobStepAsFailed(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Not all steps was completed successfully")]
    public static partial void AllStepsNotCompletedSuccessfully(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed notifying step {JobStepId}; removal: {Removing}. Delivery will be retried")]
    internal static partial void FailedNotifyingJobStep(this ILogger<IJob> logger, Exception exception, JobStepId jobStepId, bool removing);

    [LoggerMessage(LogLevel.Warning, "Job failed clearing its durable terminal state on attempt {Attempt}; after two attempts any remaining record is left to retention cleanup")]
    internal static partial void FailedClearingCompletedState(this ILogger<IJob> logger, Exception exception, int attempt);

    [LoggerMessage(LogLevel.Information, "Job ignored a stop of job step {JobStepId} from earlier execution {Generation}; the step stays tracked")]
    internal static partial void IgnoringStopOfEarlierExecution(this ILogger<IJob> logger, JobStepId jobStepId, long generation);

    [LoggerMessage(LogLevel.Information, "Job ignored an outcome of job step {JobStepId} from earlier execution {Generation}; the step is no longer tracked")]
    internal static partial void IgnoringOutcomeOfEarlierExecution(this ILogger<IJob> logger, JobStepId jobStepId, long generation);

    [LoggerMessage(LogLevel.Information, "Job is starting {Count} job steps again whose stop of an earlier execution it ignored")]
    internal static partial void RestartingStepsAfterEarlierStops(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Debug, "Job ignored an outcome of job step {JobStepId} that a drain already accounted for")]
    internal static partial void IgnoringOutcomeAccountedForByDrain(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Information, "Job is not resumed while a drain of it is pending")]
    internal static partial void NotResumingWhileDrainIsPending(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Job is stopping and draining its steps")]
    internal static partial void BeginningDrain(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Job is drained")]
    internal static partial void Drained(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Information, "Job drain timed out with {Count} job steps not drained yet")]
    internal static partial void DrainTimedOut(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Job failed draining job step {JobStepId}")]
    internal static partial void FailedDrainingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed handling the outcome of drained job step {JobStepId}")]
    internal static partial void FailedHandlingDrainedJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Information, "Job continued a pending drain after activation: {Status}")]
    internal static partial void CompletedPendingDrainAfterActivation(this ILogger<IJob> logger, JobDrainStatus status);

    [LoggerMessage(LogLevel.Warning, "Job drain found steps in storage that are neither stopped nor completed; the drain stays pending")]
    internal static partial void DrainLeftStepsUnaccounted(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed continuing a pending drain after activation")]
    internal static partial void FailedCompletingPendingDrain(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Job was not admitted at {CheckPoint}")]
    internal static partial void NotAdmitted(this ILogger<IJob> logger, string checkPoint);

    [LoggerMessage(LogLevel.Warning, "Job admission failed at {CheckPoint}; it is neither admitted nor refused")]
    internal static partial void AdmissionFailed(this ILogger<IJob> logger, Exception ex, string checkPoint);

    [LoggerMessage(LogLevel.Warning, "Job has no request to ask admission about at {CheckPoint}; admitting it")]
    internal static partial void AdmittingJobWithoutRequest(this ILogger<IJob> logger, string checkPoint);

    [LoggerMessage(LogLevel.Warning, "Job could not be stopped because the stop was not persisted; it was left as it was")]
    internal static partial void FailedStoppingJobBecauseItWasNotPersisted(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job could not be removed because the removal was not persisted; it was left as it was")]
    internal static partial void FailedRemovingJobBecauseItWasNotPersisted(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Ignoring an outcome of step {StepId} attempt {Attempt} that was already accounted for")]
    internal static partial void IgnoringDuplicateOutcome(this ILogger<IJob> logger, JobStepId stepId, int attempt);

    [LoggerMessage(LogLevel.Debug, "Ignoring a stop of step {StepId} that was already counted")]
    internal static partial void IgnoringStopAlreadyCounted(this ILogger<IJob> logger, JobStepId stepId);

    [LoggerMessage(LogLevel.Warning, "The stop of step {StepId} was counted while the job is being removed, but could not be saved; the step reports it again")]
    internal static partial void StopNotSavedWhileRemoving(this ILogger<IJob> logger, JobStepId stepId);

    [LoggerMessage(LogLevel.Warning, "Finishing the removal of the job failed with {Error}; asking to remove it again retries it")]
    internal static partial void FailedFinalizingRemoval(this ILogger<IJob> logger, JobError error);

    [LoggerMessage(LogLevel.Debug, "Starting {Count} more job steps to fill the window")]
    internal static partial void FillingWindow(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Job failed filling its window of active steps")]
    internal static partial void FailedFillingWindow(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job watchdog could not count the steps in storage")]
    internal static partial void WatchdogCouldNotCountSteps(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job watchdog failed")]
    internal static partial void WatchdogFailed(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job watchdog is restarting {Count} steps whose owner is gone")]
    internal static partial void RestartingStepsWhoseOwnerIsGone(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Information, "Running {Count} failed steps again")]
    internal static partial void RetryingFailedSteps(this ILogger<IJob> logger, int count);

    [LoggerMessage(LogLevel.Information, "Step {StepId} failed; attempt {Attempt} is scheduled in {Delay}")]
    internal static partial void SchedulingStepRetry(this ILogger<IJob> logger, JobStepId stepId, int attempt, TimeSpan delay);

    [LoggerMessage(LogLevel.Debug, "Step {StepId} reported an outcome of the removed execution after the stop of an earlier execution was counted; taking the stop back")]
    internal static partial void UncountingStopOfEarlierExecution(this ILogger<IJob> logger, JobStepId stepId);

    [LoggerMessage(LogLevel.Debug, "Step {StepId} stopped in an earlier execution; telling its current run the job is removed")]
    internal static partial void NotifyingCurrentRunOfRemoval(this ILogger<IJob> logger, JobStepId stepId);

    [LoggerMessage(LogLevel.Warning, "Job could not finish its removal; it will try again in {Delay}")]
    internal static partial void SchedulingRemovalFinalizationRetry(this ILogger<IJob> logger, TimeSpan delay);
}

public static class JobScopes
{
    public static IDisposable? BeginJobScope(this ILogger<IJob> logger, JobId jobId, JobKey key) =>
        logger.BeginScope(new
        {
            JobId = jobId,
            key.Scope,
            key.Namespace
        });
}
