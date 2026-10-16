// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Storage;

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// Counts the step state writes that reach the real step storage.
/// </summary>
/// <param name="inner">The real step storage.</param>
/// <param name="signals">The <see cref="DrainingSignals"/> to record writes in.</param>
public sealed class CountingJobStepStorage(IGrainStorage inner, DrainingSignals signals) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => inner.ReadStateAsync(stateName, grainId, grainState);

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        signals.Wrote();
        await inner.WriteStateAsync(stateName, grainId, grainState);
        if (grainState.State is JobStepState { Status: JobStepStatus.Stopped })
        {
            signals.StopPersisted.TrySetResult();
        }
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => inner.ClearStateAsync(stateName, grainId, grainState);
}
