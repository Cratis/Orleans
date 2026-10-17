// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_performing;

public class twice : given.a_job_step_with_a_test_run
{
    Exception _error;

    async Task Establish() => await _testRun.Perform(new JobStepState()).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    async Task Because() => _error = await Catch.Exception(() => _testRun.Perform(new JobStepState()));

    [Fact] void should_refuse_the_second_perform() => _error.ShouldBeOfExactType<JobStepAlreadyHasALiveRun>();
}
