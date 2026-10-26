// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A live staged job whose incomplete-stop resume failed while its second stage still had a survivor.
/// </summary>
public class a_staged_job_left_running_after_a_rejected_incomplete_stop_resume : when_running_in_stages.given.a_job_with_three_stages
{
    protected JobStepId _additionalId;
    protected Mock<ISomeJobStep> _additionalStep;
    protected long _resumedGeneration;

    protected virtual bool HasAdditionalStoppedCandidate => false;

    protected int StartsOf(Mock<ISomeJobStep> step) => step.Invocations.Count(invocation => invocation.Method.Name == nameof(IJobStep.Start));

    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        _job.Window = HasAdditionalStoppedCandidate ? 3 : 2;
        if (HasAdditionalStoppedCandidate)
        {
            _additionalId = JobStepId.New();
            _additionalStep = AddJobStep(_additionalId, 10);
            _additionalStep.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
        }
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_first));
        await Succeed(_first);
        StoredJobStep(_secondA).Status = JobStepStatus.Running;
        StoredJobStep(_secondB).Status = JobStepStatus.Running;
        if (HasAdditionalStoppedCandidate)
        {
            StoredJobStep(_additionalId).Status = JobStepStatus.Running;
        }
        await _job.Stop();
        await _job.OnStepStopped(_secondA, Stopped);
        if (!HasAdditionalStoppedCandidate)
        {
            StoredJobStep(_secondA).Status = JobStepStatus.Stopped;
        }
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("The resume hook failed"));
        await _job.Resume();
        _resumedGeneration = _job.CurrentState.StatusChanges.Count;

        // A stop reports before persisting its status. The rejected resume can have reserved its old Running slot.
        StoredJobStep(_secondA).Status = JobStepStatus.Stopped;
        if (HasAdditionalStoppedCandidate)
        {
            StoredJobStep(_additionalId).Status = JobStepStatus.Stopped;
            _additionalStep.Invocations.Clear();
        }
        _job.Window = 2;
        _job.BeforeResuming = () => Task.CompletedTask;
        foreach (var step in new[] { _firstStep, _secondStepA, _secondStepB, _thirdStep })
        {
            step.Invocations.Clear();
        }
    }
}
