// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Recovery;

[GrainType("cratis-integration-crash-recovery-job")]
[RecoveryPlacement]
public sealed class RecoveryJob(RecoverySignals signals, ILocalSiloDetails local) : Job<RecoveryRequest, JobState>, IRecoveryJob
{
    protected override bool KeepAfterCompleted => true;
    public Task<SiloAddress> Silo() => Task.FromResult(local.SiloAddress);
    public Task<JobState> Inspect() => Task.FromResult(State);

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RecoveryRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<IRecoveryStep>(request)]);

    protected override async Task OnBeforeResumingJobSteps()
    {
        if (signals.HoldFirstResume && signals.ResumeEntered.TrySetResult())
        {
            // Hold the first resume after Running is durable but before any step is restarted.
            await signals.ReleaseResume.Task;
        }
    }

    protected override Task OnAllStepsCompleted()
    {
        signals.Completed.TrySetResult();
        return Task.CompletedTask;
    }

    protected override Task OnStopped()
    {
        signals.Stopped.TrySetResult();
        return Task.CompletedTask;
    }
}
