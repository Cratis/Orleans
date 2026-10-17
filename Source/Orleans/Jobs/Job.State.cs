// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// Writes persisted state.
    /// </summary>
    /// <returns>A task returning <see cref="Catch"/>.</returns>
    protected async Task<Catch> WriteState()
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            await WriteStateAsync();
            return Catch.Success();
        }
        catch (Exception e)
        {
            _logger.FailedWritingState(e);
            return e;
        }
    }

    async Task<Result<JobError>> SetTotalSteps(int totalSteps)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            State.Progress.TotalSteps = totalSteps;
            var writeResult = await WriteState();
            return writeResult.Match(_ => Result.Success<JobError>(), ex =>
            {
                _logger.FailedToSetTotalSteps(ex);
                return JobError.PersistStateError;
            });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.ChangingStatus(status);
            StatusChanged(status, exception);
            var writeStateResult = await WriteState();
            return writeStateResult.Match(_ => Result.Success<JobError>(), _ =>
            {
                _logger.FailedWritingStatusChange(status);
                return JobError.PersistStateError;
            });
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

    /// <summary>
    /// Changes the status and writes it, undoing the change in memory when the write fails.
    /// </summary>
    /// <param name="status">The <see cref="JobStatus"/> to change to.</param>
    /// <returns>True if the change is durable, false if it was undone.</returns>
    /// <remarks>
    /// Used where what follows must only happen on a durable change - such as starting steps in an execution whose
    /// beginning a restart would not find.
    /// </remarks>
    async Task<bool> TryWriteStatusChanged(JobStatus status)
    {
        var written = await WriteOrUndo(() =>
        {
            _logger.ChangingStatus(status);
            StatusChanged(status);
        });
        return !written.TryGetException(out _);
    }

    /// <summary>
    /// Applies a change to the status or the pending drain and writes the state, undoing the change in memory when the
    /// write fails.
    /// </summary>
    /// <param name="change">The change to apply.</param>
    /// <returns>The outcome of the write.</returns>
    async Task<Catch> WriteOrUndo(Action change)
    {
        var status = State.Status;
        var statusChanges = State.StatusChanges.Count;
        var drainPending = State.DrainPending;
        change();
        var written = await WriteState();
        if (written.TryGetException(out _))
        {
            while (State.StatusChanges.Count > statusChanges)
            {
                State.StatusChanges.RemoveAt(State.StatusChanges.Count - 1);
            }
            State.Status = status;
            State.DrainPending = drainPending;
        }
        return written;
    }

    bool JobIsRunning() => State.Status is JobStatus.Running or JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps;

    bool JobIsPrepared() => State.StatusChanges.Any(change => change.Status is JobStatus.StartingSteps);

    /// <summary>
    /// Gets the generation of the job's current execution.
    /// </summary>
    /// <returns>The 1-based position in the status history of the change that began the current execution, or 0 when the job never started.</returns>
    /// <remarks>
    /// An execution begins when the job starts preparing, and when it runs again straight after a stop or a removal.
    /// Recovering a job left running continues its execution, because its live steps belong to it. Every release
    /// appends to and keeps the status history, so the generation survives a write by a release that does not know it.
    /// </remarks>
    long ExecutionGeneration()
    {
        var changes = State.StatusChanges;
        for (var position = changes.Count; position > 0; position--)
        {
            var status = changes[position - 1].Status;
            if (status is JobStatus.PreparingJob ||
                (status is JobStatus.Running && position > 1 && changes[position - 2].Status is JobStatus.Stopped or JobStatus.Removing))
            {
                return position;
            }
        }
        return 0;
    }

    bool StoppedStepsPredateLastRunning()
    {
        var statuses = State.StatusChanges.Select(change => change.Status).ToList();
        var lastStopped = statuses.LastIndexOf(JobStatus.Stopped);
        return lastStopped >= 0 && statuses.LastIndexOf(JobStatus.Running) > lastStopped && State.Progress.StoppedSteps == 0;
    }

    void StatusChanged(JobStatus status, Exception? exception = null)
    {
        State.StatusChanges.Add(new()
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionStackTrace = exception?.StackTrace ?? string.Empty,
            ExceptionMessages = exception?.GetAllMessages() ?? []
        });
        State.Status = status;
    }
}
