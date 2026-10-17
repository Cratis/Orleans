// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_resolved_parent_cannot_reply_until_stop_returns : given.a_job_step_with_a_stored_parent
{
    readonly TaskCompletionSource _reportEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<JobError>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _preservedBeforeReply;

    void Establish() => _parent.Setup(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>())).Returns(() =>
    {
        _reportEntered.TrySetResult();
        return _reply.Task;
    });

    async Task Because()
    {
        await _jobStep.OnJobStopped().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        (await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _reportEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _preservedBeforeReply = _storage.Persisted.Count == 0;
        _reply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_accept_the_stop_without_waiting_for_the_parent_reply() => _preservedBeforeReply.ShouldBeTrue();
    [Fact] void should_report_the_step_once() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Once);
    [Fact] void should_persist_stopped_after_the_parent_accepts() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);

    void Destroy() => _reply.TrySetResult(Result<JobError>.Success());
}
