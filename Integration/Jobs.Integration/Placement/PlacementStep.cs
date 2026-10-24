// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Placement;

[GrainType("cratis-integration-placement-step")]
public sealed class PlacementStep(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<PlacementStep> logger,
    PlacementSignals signals,
    ILocalSiloDetails local) : JobStep<PlacementRequest, string, JobStepState>(state, throttle, logger), IPlacementStep
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(PlacementRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());
    protected override ValueTask InitializeState(PlacementRequest request) => ValueTask.CompletedTask;
    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(JobStepState currentState) => ValueTask.FromResult<string?>(null);

    protected override async Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken)
    {
        var silo = local.SiloAddress;
        signals.Entered(silo, currentState.Id.JobStepId);

        // Deliberately ignore cancellation: killing an in-process silo must not let its old performer finish.
        await signals.GateOf(silo).Task;
        signals.Left(silo);
        return Catch<JobStepResult>.Success(JobStepResult.Succeeded("performed"));
    }
}
