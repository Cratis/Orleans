// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class with_default_retention : given.a_job_ready_to_complete
{
    Task Because() => _job.Resume();

    [Fact] void should_write_the_terminal_status_before_clearing() => Received.InOrder(() =>
    {
        _stateStorage.WriteStateAsync();
        _stateStorage.ClearStateAsync();
    });
    [Fact] void should_only_clear_after_storage_says_completed() => _statusesAtClear.ShouldContainOnly(JobStatus.CompletedSuccessfully);
    [Fact] void should_write_the_terminal_status_once() => _writeAttempts.ShouldEqual(1);
    [Fact] void should_run_the_completion_hook_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_finalization() => Deactivations.ShouldEqual(1);
}
