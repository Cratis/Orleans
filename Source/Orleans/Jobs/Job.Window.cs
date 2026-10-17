// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _started = [];
    readonly HashSet<JobStepId> _inFlight = [];

    /// <summary>
    /// Gets the maximum number of steps the job keeps active at once, or null to start every step up front.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="JobsOptions.MaxActiveStepsPerJob"/>. A job with thousands of steps overrides this to
    /// keep its window as wide as whatever its steps call into can serve. Values below 1 are treated as 1.
    /// </remarks>
    protected virtual int? MaxActiveSteps => ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value?.MaxActiveStepsPerJob;

    int? EffectiveMaxActiveSteps => MaxActiveSteps is { } max ? Math.Max(1, max) : null;

    /// <summary>
    /// Picks the steps that may be started now: the ones already started or found running in storage, and as many of
    /// the rest as the window has room for.
    /// </summary>
    /// <param name="candidates">The steps that are due to start.</param>
    /// <returns>The steps to start, marked as started.</returns>
    Dictionary<JobStepId, IJobStep> SelectWindow(Dictionary<JobStepId, IJobStep> candidates)
    {
        if (EffectiveMaxActiveSteps is not { } max)
        {
            foreach (var id in candidates.Keys)
            {
                _started.Add(id);
            }
            return candidates;
        }

        var tracked = _jobStepGrains ?? [];
        var active = tracked.Keys.Count(id => _started.Contains(id) || _inFlight.Contains(id));
        var room = Math.Max(0, max - active);
        var selected = new Dictionary<JobStepId, IJobStep>();
        foreach (var candidate in candidates)
        {
            if (_started.Contains(candidate.Key) || _inFlight.Contains(candidate.Key))
            {
                selected[candidate.Key] = candidate.Value;
            }
        }
        foreach (var candidate in candidates.Where(candidate => !selected.ContainsKey(candidate.Key)))
        {
            if (room-- <= 0)
            {
                break;
            }
            selected[candidate.Key] = candidate.Value;
        }

        foreach (var id in selected.Keys)
        {
            _started.Add(id);
        }
        return selected;
    }

    /// <summary>
    /// Forgets that a step holds a place in the window.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    void LeaveWindow(JobStepId stepId)
    {
        _started.Remove(stepId);
        _inFlight.Remove(stepId);
    }

    /// <summary>
    /// Starts the next steps of the current stage while the window has room.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    async Task FillWindow()
    {
        if (EffectiveMaxActiveSteps is null || State.DrainPending || State.Status is not (JobStatus.Running or JobStatus.StartingSteps))
        {
            return;
        }

        try
        {
            while (true)
            {
                var waiting = StepsInCurrentStage()
                    .Where(step => !_started.Contains(step.Key) && !_inFlight.Contains(step.Key) && !_pendingRetries.ContainsKey(step.Key))
                    .ToDictionary(step => step.Key, step => step.Value);
                var window = SelectWindow(waiting);
                if (window.Count == 0)
                {
                    return;
                }

                _logger.FillingWindow(window.Count);
                var started = await StartAndSubscribeToJobSteps(this.GetGrainId(), window);
                if (started.IsSuccess)
                {
                    return;
                }

                // Steps that could not start were recorded and counted as failed, which frees their place and may complete a stage or the job.
                foreach (var stepId in window.Keys)
                {
                    await AdvanceStagesAfter(stepId);
                }
                _ = await HandleCompletionResult(await HandleCompletion());
            }
        }
        catch (Exception ex)
        {
            _logger.FailedFillingWindow(ex);
        }
    }

    /// <summary>
    /// Counts the steps that were never started as stopped, since they will never report that they stopped.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    /// <remarks>
    /// Only a job with a window has such steps. They stay prepared in storage and start again when the job resumes.
    /// </remarks>
    async Task AccountForStepsThatNeverStarted()
    {
        await FlushPendingRetriesAsFailures();
        if (EffectiveMaxActiveSteps is null)
        {
            return;
        }

        var neverStarted = StepsInCurrentStage()
            .Where(step => !_started.Contains(step.Key) && !_inFlight.Contains(step.Key))
            .Select(step => step.Key)
            .ToList();
        State.Progress.StoppedSteps += neverStarted.Count;

        // The next execution begins with an empty window.
        _started.Clear();
        _inFlight.Clear();
    }
}
