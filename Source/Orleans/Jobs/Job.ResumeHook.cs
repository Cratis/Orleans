// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    const int MaxResumeRetryTicksToSkip = 15;

    // This activation holds an execution whose resume hook has not succeeded here; it gates dispatch.
    bool _resumeHookOwed;
    int _resumeHookFailures;
    int _resumeRetryTicksToSkip;

    async Task RunResumeHook()
    {
        try
        {
            await OnBeforeResumingJobSteps();
        }
        catch (Exception ex)
        {
            AdvanceResumeBackoff();
            _logger.ResumeHookFailed(ex, _resumeHookFailures);
            throw;
        }

        _resumeHookOwed = false;
        _resumeHookFailures = 0;
        _resumeRetryTicksToSkip = 0;
    }

    // An activation that has not run the resume hook (cold, or its last attempt failed) resumes itself on a watchdog
    // tick, after the startup delay gave recovery first chance; nothing dispatches until the hook has succeeded.
    async Task<bool> HoldDispatchUntilResumeHookSucceeds()
    {
        if (!_resumeHookOwed)
        {
            return false;
        }

        if (_resumeRetryTicksToSkip > 0)
        {
            _resumeRetryTicksToSkip--;
            return true;
        }

        _logger.ResumingJobWhoseResumeHookIsOwed(_resumeHookFailures);
        var failuresBefore = _resumeHookFailures;
        var result = await Resume();

        // A resume that failed before it reached the hook has not advanced the backoff; it must not retry every tick.
        if (_resumeHookOwed && !result.IsSuccess && _resumeHookFailures == failuresBefore)
        {
            AdvanceResumeBackoff();
        }

        return true;
    }

    void AdvanceResumeBackoff()
    {
        _resumeHookOwed = true;
        _resumeHookFailures++;
        _resumeRetryTicksToSkip = Math.Min((1 << Math.Min(_resumeHookFailures - 1, 4)) - 1, MaxResumeRetryTicksToSkip);
    }
}
