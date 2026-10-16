// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_recovery_is_repeated : given.a_job_left_running
{
    Result<ResumeJobSuccess, ResumeJobError> _first;
    int _performers;

    void Establish() => _unfinished.Setup(step => step.Start(It.IsAny<GrainId>())).Returns(() =>
    {
        if (_performers > 0)
        {
            return Task.FromResult(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        }
        _performers++;
        return Task.FromResult(Result<StartJobStepError>.Success());
    });

    async Task Because()
    {
        _first = await _job.Resume();
        _result = await _job.Resume();
    }

    [Fact] void should_resume_the_first_time() => ((ResumeJobSuccess)_first).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_confirm_live_work_the_second_time() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobAlreadyRunning);
    [Fact] void should_not_duplicate_performers() => _performers.ShouldEqual(1);
}
