// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs.Placement.given;

public class SomeConcreteStep(
    IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<SomeConcreteStep> logger) : JobStep<for_Job.given.SomeRequest, string, JobStepState>(state, throttle, logger)
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(for_Job.given.SomeRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());

    protected override Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken) =>
        Task.FromResult(Catch<JobStepResult>.Success(JobStepResult.Succeeded("done")));

    protected override ValueTask InitializeState(for_Job.given.SomeRequest request) => ValueTask.CompletedTask;

    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(JobStepState currentState) => ValueTask.FromResult<string?>(null);
}
