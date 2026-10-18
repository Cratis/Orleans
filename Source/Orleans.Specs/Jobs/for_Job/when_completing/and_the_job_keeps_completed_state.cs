// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_the_job_keeps_completed_state : given.a_job_ready_to_complete
{
    void Establish() => _job.ShouldBeRemovedAfterCompleted = false;

    Task Because() => _job.Resume();

    [Fact] void should_write_the_terminal_status_once() => _writtenStatuses.ShouldContainOnly(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_double_write_the_terminal_status() => _writeAttempts.ShouldEqual(1);
    [Fact] void should_not_clear_state() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_deactivate_after_persisting() => Deactivations.ShouldEqual(1);
}
