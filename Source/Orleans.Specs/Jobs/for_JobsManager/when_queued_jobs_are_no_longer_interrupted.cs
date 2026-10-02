// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_queued_jobs_are_no_longer_interrupted : given.a_controlled_backlog
{
    async Task Because()
    {
        await _manager.Rehydrate().WaitAsync(Deadline, TimeProvider.System);
        await _started[1].Task.WaitAsync(Deadline, TimeProvider.System);
        JobStatus[] statuses = [JobStatus.CompletedSuccessfully, JobStatus.CompletedWithFailures, JobStatus.Failed, JobStatus.Removing, JobStatus.None];
        for (var index = 0; index < statuses.Length; index++)
        {
            var discoveredState = _storedJobs.Single(job => job.Id == _jobIds[index + 2]);
            _storedJobs[_storedJobs.IndexOf(discoveredState)] = new JobState
            {
                Id = discoveredState.Id,
                Type = discoveredState.Type,
                Created = discoveredState.Created,
                Status = statuses[index]
            };
        }
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_not_resume_jobs_that_have_left_the_selected_statuses() => _resumed.SequenceEqual(_jobIds.Take(2)).ShouldBeTrue();
    [Fact] void should_read_the_current_state_of_every_queued_job() => _jobStorage.Received(7).GetJob(Arg.Any<JobId>());
}
