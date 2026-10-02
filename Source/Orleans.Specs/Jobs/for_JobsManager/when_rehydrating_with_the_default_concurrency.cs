// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_with_the_default_concurrency : given.a_controlled_backlog
{
    int _initialResumeCount;

    protected override int BacklogSize => 10;

    void Establish() => _options.Value.Returns(new JobsOptions());

    async Task Because()
    {
        await _manager.Rehydrate();
        await _started[7].Task.WaitAsync(Deadline, TimeProvider.System);
        _initialResumeCount = _resumed.Count;
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_initially_resume_eight_jobs() => _initialResumeCount.ShouldEqual(8);
    [Fact] void should_never_exceed_eight_concurrent_resumes() => _maximumConcurrentResumes.ShouldEqual(8);
    [Fact] void should_resume_every_job() => _resumed.OrderBy(id => id.Value).SequenceEqual(_jobIds.OrderBy(id => id.Value)).ShouldBeTrue();
}
