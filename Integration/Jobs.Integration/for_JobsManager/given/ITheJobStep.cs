// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

internal interface ITheJobStep : IJobStep<TheJobStepRequest, TheJobStepResult, TheJobStepState>
{
    [AlwaysInterleave]
    public Task SetCompleted();
    [AlwaysInterleave]
    public Task SetFailed();
    [AlwaysInterleave]
    public Task IncrementStopped();
    [AlwaysInterleave]
    public Task IncrementPerformCalled();
}
