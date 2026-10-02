// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.given;

public class a_controlled_backlog : the_manager
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected readonly List<JobId> _jobIds = [];
    protected readonly List<Mock<INullJobWithSomeRequest>> _jobs = [];
    protected readonly ConcurrentQueue<JobId> _resumed = new();
    protected readonly List<TaskCompletionSource> _started = [];
    protected readonly List<TaskCompletionSource<Result<ResumeJobSuccess, ResumeJobError>>> _completions = [];
    protected int _maximumConcurrentResumes;
    int _activeResumes;

    protected virtual int BacklogSize => 7;

    void Establish()
    {
        _options.Value.Returns(new JobsOptions { MaxConcurrentRehydration = 2 });
        var created = DateTimeOffset.UtcNow.AddMinutes(-10);
        for (var index = 0; index < BacklogSize; index++)
        {
            var id = JobId.New();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var completion = new TaskCompletionSource<Result<ResumeJobSuccess, ResumeJobError>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _jobIds.Add(id);
            _started.Add(started);
            _completions.Add(completion);
            var job = AddJob<INullJobWithSomeRequest>(id);
            _jobs.Add(job);
            _storedJobs[^1].Created = created.AddSeconds(index);
            job.Setup(job => job.Resume()).Returns(async () =>
            {
                var active = Interlocked.Increment(ref _activeResumes);

                // TestKit does not supply the activation's single-threaded scheduler.
                int maximum;
                while (active > (maximum = Volatile.Read(ref _maximumConcurrentResumes)) &&
                       Interlocked.CompareExchange(ref _maximumConcurrentResumes, active, maximum) != maximum)
                {
                }
                _resumed.Enqueue(id);
                started.SetResult();
                try
                {
                    return await completion.Task;
                }
                finally
                {
                    Interlocked.Decrement(ref _activeResumes);
                }
            });
        }
        _storedJobs.Reverse();
    }

    protected void CompleteResumes()
    {
        foreach (var completion in _completions)
        {
            completion.TrySetResult(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        }
    }
}
