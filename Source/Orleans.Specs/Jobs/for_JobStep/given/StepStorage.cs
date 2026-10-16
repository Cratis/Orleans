// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Orleans.Core;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// Step storage that records what reached it and can hold a write open.
/// </summary>
/// <param name="state">The state the step loads.</param>
public class StepStorage(JobStepState state) : IStorage<JobStepState>
{
    readonly List<PersistedStep> _persisted = [];

    /// <inheritdoc/>
    public JobStepState State { get; set; } = state;

    /// <inheritdoc/>
    public string? Etag => null;

    /// <inheritdoc/>
    public bool RecordExists => true;

    /// <summary>
    /// Gets or sets the gate the next writes wait on before they reach storage; null lets writes through.
    /// </summary>
    public TaskCompletionSource? HoldWrites { get; set; }

    /// <summary>
    /// Gets the signal that a write is being held.
    /// </summary>
    public TaskCompletionSource WriteHeld { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets what reached storage, in order.
    /// </summary>
    public IReadOnlyList<PersistedStep> Persisted
    {
        get
        {
            lock (_persisted)
            {
                return [.. _persisted];
            }
        }
    }

    /// <inheritdoc/>
    public Task ClearStateAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ReadStateAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task WriteStateAsync()
    {
        if (HoldWrites is { } hold)
        {
            WriteHeld.TrySetResult();
            await hold.Task;
        }
        lock (_persisted)
        {
            _persisted.Add(new(State.Status, State.Progress.Message.Value));
        }
    }
}
