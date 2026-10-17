// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_performing;

public class after_a_later_test_run_began : given.a_job_step_with_a_test_run
{
    JobStepTestRun<JobStepState> _laterTestRun;
    Exception _error;
    int _performed;

    void Establish()
    {
        _jobStep.Work = (_, _) =>
        {
            Interlocked.Increment(ref _performed);
            return Task.CompletedTask;
        };
        _laterTestRun = _jobStep.BeginTestRun();
    }

    async Task Because() => _error = await Catch.Exception(() => _testRun.Perform(new JobStepState()));

    [Fact] void should_refuse_to_perform() => _error.ShouldBeOfExactType<JobStepTestRunWasSuperseded>();
    [Fact] void should_not_perform_the_step() => _performed.ShouldEqual(0);
    [Fact] void should_leave_the_later_run_admitting_callbacks() => _laterTestRun.AdmissionOpen.ShouldBeTrue();
}
