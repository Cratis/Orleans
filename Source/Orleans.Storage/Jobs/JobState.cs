// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Holds the state of a job.
/// </summary>
public class JobState
{
    /// <summary>
    /// Gets or sets the <see cref="JobId"/>.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the details for a job.
    /// </summary>
    public JobDetails Details { get; set; } = JobDetails.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobType"/>.
    /// </summary>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the time that this job was created.
    /// </summary>
    public DateTimeOffset Created { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    public IList<JobStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether a drain of the job has begun and not settled yet.
    /// </summary>
    /// <remarks>
    /// Written together with the stop that begins the drain, so a job that went away mid-drain completes it when it
    /// is activated again and is not resumed before then.
    /// </remarks>
    public bool DrainPending { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current stop of the job has been finalized by running its stopped hook.
    /// </summary>
    /// <remarks>
    /// Any change of status clears it, so every stop is finalized once. A job written before this was introduced reads
    /// as not finalized, so a stopped job left behind by a silo that went away is finalized once when it is rehydrated.
    /// </remarks>
    public bool StopFinalized { get; set; }

    /// <summary>
    /// Gets or sets the steps whose stop the current execution of the job has counted.
    /// </summary>
    /// <remarks>
    /// Saved together with the count of stopped steps, so a step that reports its stop again - because it did not hear
    /// back, or because the job was activated again in between - is counted once per execution. An execution that
    /// begins anew starts with none. A job written before this was introduced reads with none counted.
    /// </remarks>
    public IList<JobStepId> StoppedStepsCounted { get; set; } = [];

    /// <summary>
    /// Gets or sets the steps whose success or failure was counted in an execution begun by a resume or reopen.
    /// </summary>
    /// <remarks>
    /// Entries are kept across resumes and CompletedWithFailures, and removed only when RetryFailed uncounts that
    /// failure. Custom storage providers must persist this list. State saved without it reads as empty.
    /// A never-resumed job records no entries.
    /// </remarks>
    public IList<JobStepId> CompletedStepsCounted { get; set; } = [];

    /// <summary>
    /// Gets or sets the complete step plan saved before preparing any step.
    /// </summary>
    /// <remarks>
    /// Null means no plan was saved, including jobs written before durable planning was introduced.
    /// An empty plan is distinct from a missing plan.
    /// </remarks>
    public IReadOnlyList<PlannedJobStep>? StepPlan { get; set; }

    /// <summary>
    /// Gets or sets the request associated with the job.
    /// </summary>
    public IJobRequest Request { get; set; } = default!;

    /// <summary>
    /// Whether the job is preparing to be started or is in a running state.
    /// </summary>
    public bool IsPreparingOrRunning => Status is JobStatus.Running or JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps;
}
