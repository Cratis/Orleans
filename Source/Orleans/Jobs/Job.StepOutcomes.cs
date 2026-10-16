// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    async Task<bool> IgnoreLateStepOutcome(JobStepId stepId)
    {
        if (AcceptsStepOutcome(stepId))
        {
            return false;
        }

        _logger.IgnoringLateStepOutcome(stepId, State.Status);
        await UnsubscribeJobStep(stepId);
        _jobStepGrains?.Remove(stepId);
        return true;
    }

    bool AcceptsStepOutcome(JobStepId stepId)
    {
        // Missing and cleared records have no preparation history. Never create a job from a late report.
        if (!JobIsPrepared())
        {
            return false;
        }

        return State.Status switch
        {
            JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing => true,

            // Stop is durable before its steps report, so Stopped alone cannot reject pending outcomes.
            // Once fully stopped, or once this activation accepted this step's report, it must not count again.
            JobStatus.Stopped => !State.Progress.IsStopped && (!_hasLiveStepTracking || _jobStepGrains?.ContainsKey(stepId) == true),
            _ => false
        };
    }
}
