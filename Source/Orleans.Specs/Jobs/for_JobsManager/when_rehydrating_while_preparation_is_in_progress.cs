// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_while_preparation_is_in_progress : given.a_controlled_backlog
{
    TaskCompletionSource<Catch<IImmutableList<JobState>>> _discovery;
    bool _bothWaitedForDiscovery;

    void Establish()
    {
        _discovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobStorage.GetJobs(JobStatus.Running, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps).Returns(_discovery.Task);
    }

    async Task Because()
    {
        var firstCall = _manager.Rehydrate();
        var secondCall = _manager.Rehydrate();
        _bothWaitedForDiscovery = !firstCall.IsCompleted && !secondCall.IsCompleted;
        _discovery.SetResult(Catch.Success<IImmutableList<JobState>>([.. _storedJobs]));
        await Task.WhenAll(firstCall, secondCall).WaitAsync(Deadline, TimeProvider.System);
        CompleteResumes();
        await WaitForRehydration();
    }

    [Fact] void should_finish_preparation_before_returning() => _bothWaitedForDiscovery.ShouldBeTrue();
    [Fact] void should_only_discover_the_backlog_once() => _jobStorage.Received(1).GetJobs(JobStatus.Running, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps);
    [Fact] void should_only_resume_each_job_once() => _resumed.OrderBy(id => id.Value).SequenceEqual(_jobIds.OrderBy(id => id.Value)).ShouldBeTrue();
}
