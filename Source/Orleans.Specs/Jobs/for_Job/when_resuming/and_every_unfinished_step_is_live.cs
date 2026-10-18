// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_every_unfinished_step_is_live : given.a_job_left_running
{
    void Establish() => _unfinished.Setup(step => step.Start(It.IsAny<GrainId>()))
        .ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_confirm_liveness_before_reporting_already_running() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_report_recovery_on_a_fresh_activation() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_preserve_the_status_history() => _job.CurrentState.StatusChanges.Count.ShouldEqual(1);
}
