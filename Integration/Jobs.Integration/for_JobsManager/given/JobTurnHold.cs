// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

/// <summary>
/// Lets a scenario keep a job grain busy in a turn of its own, so it controls the order in which the job sees
/// the calls queued up behind it.
/// </summary>
public class JobTurnHold
{
    TaskCompletionSource _holding = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a task that completes once the job is holding its turn.
    /// </summary>
    public Task Holding => _holding.Task;

    /// <summary>
    /// Gets or sets the job that was started, as addressed by the jobs manager that started it.
    /// </summary>
    internal IJobWithHeldTurn? Job { get; set; }

    /// <summary>
    /// Returns the hold to the state it was constructed in.
    /// </summary>
    public void Reset()
    {
        _release.TrySetResult();
        Job = null;
        _holding = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Lets the job's held turn finish.
    /// </summary>
    public void Release() => _release.TrySetResult();

    /// <summary>
    /// Called from within the job's turn: signals that the turn is held and waits until it is released.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Hold()
    {
        _holding.TrySetResult();
        return _release.Task;
    }
}
