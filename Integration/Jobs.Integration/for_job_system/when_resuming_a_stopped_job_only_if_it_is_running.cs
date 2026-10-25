// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_resuming_a_stopped_job_only_if_it_is_running : Specification
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

        _result = await job.ResumeIfRunning();
        _state = await job.Inspect();
    }

    [Fact] void should_report_that_the_job_is_not_running() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsNotRunning);
    [Fact] void should_leave_the_job_stopped() => _state.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_perform_the_step_again() => _fixture.Signals.Performers.Count.ShouldEqual(1);
    [Fact] void should_not_run_the_stopped_hook_again() => _fixture.Signals.StoppedCalls.ShouldEqual(_stoppedCallsBeforeResume);

    void Destroy() => _fixture.Dispose();
}
