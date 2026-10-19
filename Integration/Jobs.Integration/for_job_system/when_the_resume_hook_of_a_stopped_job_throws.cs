// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_the_resume_hook_of_a_stopped_job_throws : Specification
{
    RecoveryClusterFixture _fixture;
    Result<ResumeJobSuccess, ResumeJobError> _result;
    int _stoppedCallsBeforeResume;
    JobState _state;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (_, job) = await _fixture.Start();
        (await job.Stop()).IsSuccess.ShouldBeTrue();
        _fixture.Signals.ReleaseOriginal.TrySetResult();
        await _fixture.Signals.Stopped.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _stoppedCallsBeforeResume = _fixture.Signals.StoppedCalls;

        _fixture.Signals.ThrowOnResume = true;
        _result = await job.Resume();
        _state = await job.Inspect();
    }

    [Fact] void should_fail_the_resume() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_put_the_job_back_to_stopped() => _state.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_keep_the_step_counted_as_stopped() => _state.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_run_the_stopped_hook_again() => _fixture.Signals.StoppedCalls.ShouldEqual(_stoppedCallsBeforeResume + 1);
    [Fact] void should_not_perform_the_step_again() => _fixture.Signals.Performers.Count.ShouldEqual(1);

    void Destroy() => _fixture.Dispose();
}
