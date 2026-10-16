// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Monads;
using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class and_dispatch_is_refused_by_a_retained_worker : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _result;

    void Establish()
    {
        _persistedState.Status = JobStepStatus.Unknown;

        // A retained worker task refuses dispatch, even though it is no longer Running.
        typeof(GrainWithBackgroundTask<given.SomeRequest, JobStepResult>)
            .GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_jobStep, Task.CompletedTask);
    }

    async Task Because() => _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));

    [Fact] void should_report_a_dispatch_failure() => _result.AsT1.ShouldEqual(StartJobStepError.Unknown);
    [Fact] void should_restore_the_previous_status() => _persistedState.Status.ShouldEqual(JobStepStatus.Unknown);
    [Fact] void should_not_dispatch_work() => _jobStep.WorkEntered.Task.IsCompleted.ShouldBeFalse();

    void Destroy() => _jobStep.Dispose();
}
