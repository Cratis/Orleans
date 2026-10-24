// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Placement;

[GrainType("cratis-integration-placement-job")]
public sealed class PlacementJob(PlacementSignals signals, ILocalSiloDetails local) : Job<PlacementRequest, JobState>, IPlacementJob
{
    protected override bool KeepAfterCompleted => true;
    public Task<SiloAddress> Silo() => Task.FromResult(local.SiloAddress);
    public Task<JobState> Inspect() => Task.FromResult(State);

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(PlacementRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            Enumerable.Range(0, request.Steps).Select(_ => CreateStep<IPlacementStep>(request)).ToImmutableList());

    protected override Task OnAllStepsCompleted()
    {
        signals.Completed.TrySetResult();
        return Task.CompletedTask;
    }
}
