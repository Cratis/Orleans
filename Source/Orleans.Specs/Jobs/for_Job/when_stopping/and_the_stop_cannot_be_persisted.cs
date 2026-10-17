// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_stopping;

public class and_the_stop_cannot_be_persisted : given.a_running_job_with_steps
{
    Result<StopJobError> _result;

    void Establish() => _job.FailWrites = true;

    async Task Because() => _result = await _job.Stop();

    [Fact] void should_refuse_the_stop() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_say_that_it_could_not_persist_the_stop() => (_result.TryGetError(out var refusal) ? refusal : StopJobError.Unknown).ShouldEqual(StopJobError.CouldNotPersistStop);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_remember_a_stop_that_never_happened() => _job.CurrentState.StatusChanges.Any(_ => _.Status == JobStatus.Stopped).ShouldBeFalse();
}
