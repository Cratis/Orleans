// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

[GrainType("cratis-integration-crash-recovery-step")]
[RecoveryPlacement]
public sealed class RecoveryStep(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<RecoveryStep> logger,
    RecoverySignals signals,
    ILocalSiloDetails local) : JobStep<RecoveryRequest, string, JobStepState>(state, throttle, logger), IRecoveryStep
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(RecoveryRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());
    protected override ValueTask InitializeState(RecoveryRequest request) => ValueTask.CompletedTask;
    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(JobStepState currentState) => ValueTask.FromResult<string?>(null);

    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        var result = await base.PerformWork();
        if (result.Error is PerformWorkError.Cancelled)
        {
            signals.StepStopped.TrySetResult();
        }
        return result;
    }

    protected override async Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken)
    {
        var silo = local.SiloAddress;
        signals.Performers.Enqueue(silo);
        signals.PerformerSteps.Enqueue(currentState.Id.JobStepId);
        if (signals.FirstEntry.TrySetResult(silo))
        {
            // Deliberately ignore cancellation: killing an in-process silo must not let its old performer
            // complete and masquerade as a successful survivor. Only the stop variant releases this gate.
            await signals.ReleaseOriginal.Task;
        }
        else
        {
            signals.SecondEntry.TrySetResult(silo);
            await signals.ReleaseRecovered.Task;
        }
        return Catch<JobStepResult>.Success(cancellationToken.IsCancellationRequested
            ? JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult())
            : JobStepResult.Succeeded("performed"));
    }
}
