// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_outcome_was_recorded : given.a_stopped_job_with_a_recorded_outcome
{
    void Establish() => _job.BeforeResuming = null;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_record() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_completedId);
    [Fact] void should_save_the_record() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_completedId);
}
