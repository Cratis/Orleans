// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public class JobWithSingleStep : Job<JobWithSingleStepRequest, JobWithSingleStepState>, IJobWithSingleStep
{
    bool _keepAfterCompleted = true;
    protected override bool KeepAfterCompleted => _keepAfterCompleted;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(JobWithSingleStepRequest request)
    {
        _keepAfterCompleted = request.KeepAfterCompleted;

        return Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<ITheJobStep>(new TheJobStepRequest(
            request.ShouldFail, request.WaitTime ?? TimeSpan.Zero, request.WaitCount))]);
    }
}
