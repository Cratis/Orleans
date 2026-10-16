// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_status_write_fails : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    int _statusChanges;

    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.ShouldBeResumable = true;
        _statusChanges = _job.CurrentState.StatusChanges.Count;
        _job.FailWrites = true;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_start_any_step_again() => _first.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_keep_the_unwritten_status_change() => _job.CurrentState.StatusChanges.Count.ShouldEqual(_statusChanges);
}
