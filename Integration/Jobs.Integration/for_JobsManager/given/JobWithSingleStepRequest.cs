// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public record JobWithSingleStepRequest(bool KeepAfterCompleted = false, bool ShouldFail = false, TimeSpan? WaitTime = null, int WaitCount = 0) : IJobRequest;
