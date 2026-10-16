// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// A step that keeps working until it is cancelled, placed on the silo a spec chooses.
/// </summary>
/// <param name="state">The persisted state.</param>
/// <param name="throttle">The throttle.</param>
/// <param name="logger">The logger.</param>
/// <param name="signals">The <see cref="DrainingSignals"/>.</param>
/// <param name="local">The details of the silo hosting the step.</param>
[GrainType("cratis-integration-draining-step")]
[RecoveryPlacement]
public sealed class DrainingStep(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<DrainingStep> logger,
    DrainingSignals signals,
    ILocalSiloDetails local) : JobStep<DrainingRequest, string, JobStepState>(state, throttle, logger), IDrainingStep
{
    /// <summary>
    /// The progress message the step checkpoints once it has been cancelled.
    /// </summary>
    public const string FinalCheckpointMessage = "final";

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        signals.Activated();
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(DrainingRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(DrainingRequest request) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(JobStepState currentState) => ValueTask.FromResult<string?>(null);

    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        try
        {
            return await base.PerformWork();
        }
        finally
        {
            signals.WorkerSettled.TrySetResult();
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken)
    {
        await Checkpoint("started");
        signals.Entered.TrySetResult(local.SiloAddress);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is how the step is asked to finish; it checkpoints and returns a stopped outcome below.
        }
        signals.Cancelled.TrySetResult();
        if (signals.HoldAfterCancellation)
        {
            await signals.ReleaseWork.Task;
        }
        signals.FinalCheckpoint.TrySetResult(await Checkpoint(FinalCheckpointMessage));
        return Catch<JobStepResult>.Success(JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
    }

    Task<Result<ActivationCallbackRejected>> Checkpoint(string message) =>
        TryInvokeOnActivation(async () =>
        {
            State.Progress.Message = new(message);
            await WriteStateAsync();
        });
}
