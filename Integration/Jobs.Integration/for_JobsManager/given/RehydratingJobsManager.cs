// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

[Reentrant]
[GrainType("test-rehydrating-jobs-manager")]
public class RehydratingJobsManager(
    IJobsStorage jobsStorage,
    IJobTypes jobTypes,
    IOptions<JobsOptions> options,
    ILogger<JobsManager> logger,
    RehydrationLifecycle lifecycle) : JobsManager(jobsStorage, jobTypes, options, logger), IRehydratingJobsManager
{
    public async Task DeactivateWhileBusy()
    {
        // Observe the private background task without adding a test-only public API to JobsManager.
        // This real request stays active after DeactivateOnIdle, preventing OnDeactivateAsync from being
        // called until after the drain stops. Its cancellation hook therefore cannot make the test pass.
        var drain = (Task)typeof(JobsManager).GetField("_rehydration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
        DeactivateOnIdle();
        lifecycle.DeactivationRequested.SetResult();
        await drain;
        lifecycle.DrainStopped.SetResult();
        await lifecycle.FinishRequest.Task;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        lifecycle.OnDeactivateCalled.TrySetResult();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
