// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_again_after_a_drain_completes : given.a_controlled_backlog
{
    Mock<INullJobWithSomeRequest> _newJob;

    async Task Establish()
    {
        CompleteResumes();
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await WaitForRehydration();
        _rehydrationFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _storedJobs.Clear();
        _newJob = AddJob<INullJobWithSomeRequest>(JobId.New());
        _newJob.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await WaitForRehydration();
    }

    [Fact] void should_discover_and_resume_new_interrupted_jobs() => _newJob.Verify(job => job.Resume(), Times.Once);
}
