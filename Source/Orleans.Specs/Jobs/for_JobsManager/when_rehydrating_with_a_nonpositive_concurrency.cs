// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_with_a_nonpositive_concurrency : given.a_controlled_backlog
{
    int _initialResumeCount;

    void Establish() => _options.Value.Returns(new JobsOptions { MaxConcurrentRehydration = -1 });

    async Task Because()
    {
        await _manager.Rehydrate();
        await _started[0].Task.WaitAsync(Deadline, TimeProvider.System);
        _initialResumeCount = _resumed.Count;
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_initially_resume_one_job() => _initialResumeCount.ShouldEqual(1);
    [Fact] void should_never_exceed_one_concurrent_resume() => _maximumConcurrentResumes.ShouldEqual(1);
    [Fact] void should_resume_every_job() => _resumed.SequenceEqual(_jobIds).ShouldBeTrue();
}
