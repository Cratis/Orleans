// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_with_failed_resumes : given.a_controlled_backlog
{
    async Task Because()
    {
        await _manager.Rehydrate();
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        _completions[0].SetException(new Exception("Job activation failed"));
        _completions[1].SetResult(Result<ResumeJobSuccess, ResumeJobError>.Failed(CannotResumeJobError.JobCannotBeResumed));
        await _started[3].Task.WaitAsync(Deadline, TimeProvider.System);
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_continue_past_both_exceptions_and_refused_resumes() => _resumed.OrderBy(id => id.Value).SequenceEqual(_jobIds.OrderBy(id => id.Value)).ShouldBeTrue();
    [Fact] void should_keep_the_concurrency_bound_after_failures() => _maximumConcurrentResumes.ShouldEqual(2);
}
