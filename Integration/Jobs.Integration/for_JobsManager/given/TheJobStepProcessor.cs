// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

/// <summary>
/// Records what the steps of a job actually did, so a scenario can assert on the work rather than only on
/// the state left behind.
/// </summary>
/// <remarks>
/// One instance is registered in the shared silo, because the step grains resolve it from there. Every
/// scenario calls <see cref="Reset"/> first, so what a previous scenario recorded cannot be mistaken for
/// its own.
/// </remarks>
public class TheJobStepProcessor
{
    public class PreparedJobSteps(IEnumerable<(JobStepId JobStepId, Type JobStepType)> list) : List<(JobStepId JobStepId, Type JobStepType)>(list);

    public class PerformedJobStepCalls : Dictionary<JobStepId, IList<TheJobStepState>>;
    public class CompletedJobSteps : Dictionary<JobStepId, (TheJobStepState State, JobStepStatus Status)>;

    TaskCompletionSource _allPreparedJobsStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _jobsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Task _startTask = Task.CompletedTask;
    int _numJobStepsToComplete;

    volatile int _numJobStepsPrepared;
    volatile int _numJobStepsStarted;
    volatile int _numJobStepsCompleted;

    /// <summary>
    /// Returns the processor to the state it was constructed in.
    /// </summary>
    /// <remarks>
    /// The completion sources are replaced rather than reused: a <see cref="TaskCompletionSource"/> that has
    /// already been completed stays completed, so a scenario would wait on the previous one's result and
    /// pass without its own steps ever running.
    /// </remarks>
    public void Reset()
    {
        _allPreparedJobsStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _startTask = Task.CompletedTask;
        _numJobStepsToComplete = 0;
        _numJobStepsPrepared = 0;
        _numJobStepsStarted = 0;
        _numJobStepsCompleted = 0;
        JobSteps.Clear();
        JobStepPerformCalls.Clear();
        CompletedSteps.Clear();
    }

    public readonly ConcurrentDictionary<JobId, PreparedJobSteps> JobSteps = new();
    public readonly ConcurrentDictionary<JobId, PerformedJobStepCalls> JobStepPerformCalls = new();
    public readonly ConcurrentDictionary<JobId, CompletedJobSteps> CompletedSteps = new();

    public void SetNumJobStepsToComplete(int numJobSteps) => _numJobStepsToComplete = numJobSteps;
    public void SetStartTask(Task startTask) => _startTask = startTask;
    public Task WaitForStart() => _startTask;
    public Task WaitForAllPreparedStepsToBeStarted(TimeSpan? maxWaitTime = null) => _allPreparedJobsStarted.Task.WaitAsync(maxWaitTime ?? TimeSpan.FromSeconds(30));
    public Task WaitForStepsToBeCompleted(TimeSpan? maxWaitTime = null) => _jobsCompleted.Task.WaitAsync(maxWaitTime ?? TimeSpan.FromSeconds(30));

    public IEnumerable<JobStepId> GetJobStepsForJob(JobId jobId)
    {
        if (!JobSteps.TryGetValue(jobId, out var jobSteps))
        {
            return [];
        }
        return jobSteps.Select(_ => _.JobStepId);
    }

    public int GetNumPerformCallsFor(JobId jobId, JobStepId jobStepId)
    {
        if (!JobStepPerformCalls.TryGetValue(jobId, out var jobSteps))
        {
            return 0;
        }
        return !jobSteps.TryGetValue(jobStepId, out var jobStepCalls)
            ? 0
            : jobStepCalls.Count;
    }

    public Dictionary<JobStepId, int> GetNumPerformCallsPerJobStep(JobId jobId)
    {
        if (!JobStepPerformCalls.TryGetValue(jobId, out var jobSteps))
        {
            return [];
        }
        return jobSteps.ToDictionary(_ => _.Key, _ => _.Value.Count);
    }

    public CompletedJobSteps GetCompletedJobSteps(JobId jobId)
    {
        if (!CompletedSteps.TryGetValue(jobId, out var completedJobSteps))
        {
            return [];
        }
        return completedJobSteps;
    }

#pragma warning disable MA0106
    public void JobStepPrepared(JobId JobId, JobStepId jobStepId, Type jobStepType)
    {
        JobSteps.AddOrUpdate(JobId, _ => new PreparedJobSteps([(jobStepId, jobStepType)]), (_, jobSteps) => new(jobSteps.Concat([(jobStepId, jobStepType)])));
        Interlocked.Increment(ref _numJobStepsPrepared);
    }
#pragma warning restore MA0106

    public void PerformJobStep(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState)
    {
#pragma warning disable MA0106
        JobStepPerformCalls.AddOrUpdate(
            jobId,
            _ => new PerformedJobStepCalls
            {
                {
                    jobStepId, new List<TheJobStepState>
                    {
                        jobStepState
                    }
                }
            },
            (id, states) =>
            {
                if (!states.TryGetValue(jobStepId, out var jobStepStates))
                {
                    jobStepStates = [];
                }
                jobStepStates.Add(jobStepState);
                states[jobStepId] = jobStepStates;
                return states;
            });
#pragma warning restore MA0106

        Interlocked.Increment(ref _numJobStepsStarted);
        if (_numJobStepsStarted >= _numJobStepsPrepared)
        {
            _allPreparedJobsStarted.TrySetResult();
        }
    }

    public void JobStepCompleted(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState, JobStepStatus status)
    {
#pragma warning disable MA0106
        CompletedSteps.AddOrUpdate(
            jobId,
            _ => new CompletedJobSteps
            {
                {
                    jobStepId, (jobStepState, status)
                }
            },
            (id, states) =>
            {
                states[jobStepId] = (jobStepState, status);
                return states;
            });
#pragma warning restore MA0106
        Interlocked.Increment(ref _numJobStepsCompleted);
        if (_numJobStepsCompleted >= _numJobStepsToComplete)
        {
            _jobsCompleted.TrySetResult();
        }
    }
}
