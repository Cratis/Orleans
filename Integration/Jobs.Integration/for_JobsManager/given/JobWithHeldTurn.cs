// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public class JobWithHeldTurn : Job<JobWithSingleStepRequest, JobWithSingleStepState>, IJobWithHeldTurn
{
    protected override bool KeepAfterCompleted => true;

    public Task HoldTurn() => ServiceProvider.GetRequiredService<JobTurnHold>().Hold();

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(JobWithSingleStepRequest request)
    {
        ServiceProvider.GetRequiredService<JobTurnHold>().Job = this.AsReference<IJobWithHeldTurn>();
        return Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<ITheJobStep>(new TheJobStepRequest(
            request.ShouldFail, request.WaitTime ?? TimeSpan.Zero, request.WaitCount))]);
    }
}
