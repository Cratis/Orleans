// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_retrying;

public class and_the_step_failed_its_first_attempt : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _result;

    void Establish()
    {
        _persistedState.Status = JobStepStatus.CompletedWithFailure;
        _persistedState.Attempt = 1;
    }

    async Task Because() => _result = await _jobStep.Retry(GrainId.Create("job", _jobId.ToString()), 2);

    [Fact] void should_start_the_attempt() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_on_the_second_attempt() => _persistedState.Attempt.ShouldEqual(2);
    [Fact] void should_schedule_it() => _storage.Persisted[0].Status.ShouldEqual(JobStepStatus.Scheduled);
    [Fact] void should_persist_the_attempt_with_the_schedule() => _storage.Persisted[0].Attempt.ShouldEqual(2);

    [Fact]
    async Task should_perform_the_step_again()
    {
        var performed = await Task.WhenAny(_jobStep.Performed.Task, Task.Delay(TimeSpan.FromSeconds(5), TimeProvider.System, TestContext.Current.CancellationToken));
        performed.ShouldEqual(_jobStep.Performed.Task);
    }

    void Destroy() => _jobStep.Dispose();
}
