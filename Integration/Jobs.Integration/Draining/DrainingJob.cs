// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// A job with one step that keeps working until it is cancelled, placed on the silo a spec chooses.
/// </summary>
/// <param name="signals">The <see cref="DrainingSignals"/>.</param>
[GrainType("cratis-integration-draining-job")]
[RecoveryPlacement]
public sealed class DrainingJob(DrainingSignals signals) : Job<DrainingRequest, JobState>, IDrainingJob
{
    /// <inheritdoc/>
    protected override bool KeepAfterCompleted => true;

    /// <inheritdoc/>
    public Task<JobState> Inspect() => Task.FromResult(State);

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(DrainingRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<IDrainingStep>(request)]);

    /// <inheritdoc/>
    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        signals.StepOutcomeRecorded.TrySetResult();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnAllStepsCompleted()
    {
        signals.JobCompleted.TrySetResult();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnStopped()
    {
        signals.JobStopped.TrySetResult();
        return Task.CompletedTask;
    }
}
