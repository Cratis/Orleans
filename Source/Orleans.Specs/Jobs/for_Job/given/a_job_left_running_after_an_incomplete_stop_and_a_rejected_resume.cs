// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A live job whose first step stopped, whose other steps survived, and whose incomplete-stop resume hook failed.
/// </summary>
public class a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume : a_running_job_with_steps
{
    protected const string ReportedGenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    protected long _earlierGeneration;
    protected long _resumedGeneration;
    protected int _writesAfterRejectedResume;
    protected int _stoppedWritesBeforeResume;
    protected int _stoppedWritesAfterRejectedResume;
    protected bool _rejectedResumeFailed;
    protected List<string> _dispatchOrder = [];
    protected List<(int Step, long? Generation)> _startsInGeneration = [];

    protected void AllowResume() => _job.BeforeResuming = () =>
    {
        _dispatchOrder.Add("resume");
        return Task.CompletedTask;
    };

    protected long? GenerationOfStart(int index) => _startsInGeneration.Single(start => start.Step == index).Generation;

    protected bool HooksPrecedeStarts() => _dispatchOrder.Contains("resume") && _dispatchOrder.Contains("dispatch") &&
        _dispatchOrder.IndexOf("resume") < _dispatchOrder.IndexOf("dispatch") &&
        _dispatchOrder.Where(entry => entry.StartsWith("start:", StringComparison.Ordinal))
            .All(entry => _dispatchOrder.IndexOf("dispatch") < _dispatchOrder.IndexOf(entry));

    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        _earlierGeneration = _job.CurrentState.StatusChanges.Count;
        await _job.Stop();
        await _job.OnStepStopped(_stepIds[0], Stopped);
        StoredJobStep(_stepIds[0]).Status = JobStepStatus.Stopped;
        _stoppedWritesBeforeResume = _job.WrittenStates.Count(state => state.Status is JobStatus.Stopped);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("The resume hook failed"));
        _rejectedResumeFailed = !(await _job.Resume()).IsSuccess;
        _resumedGeneration = _job.CurrentState.StatusChanges.Count;
        _writesAfterRejectedResume = Writes;
        _stoppedWritesAfterRejectedResume = _job.WrittenStates.Count(state => state.Status is JobStatus.Stopped);
        _job.BeforeDispatching = _ =>
        {
            _dispatchOrder.Add("dispatch");
            return Task.CompletedTask;
        };
        for (var index = 0; index < _steps.Count; index++)
        {
            var stepIndex = index;
            _steps[index].Setup(step => step.Start(It.IsAny<GrainId>())).Returns(() =>
            {
                _dispatchOrder.Add($"start:{stepIndex}");
                _startsInGeneration.Add((stepIndex, RequestContext.Get("Cratis.Orleans.Jobs.Generation") as long?));
                StoredJobStep(_stepIds[stepIndex]).Status = JobStepStatus.Running;
                return Task.FromResult(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
            });
            _steps[index].Invocations.Clear();
        }
    }
}
