// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_rehydrating_many;

public class with_more_jobs_than_the_concurrency_limit : given.the_manager
{
    const int JobCount = 20;
    const int Limit = 3;

    int _inFlight;
    int _peak;
    readonly List<JobId> _resumeOrder = [];
    readonly List<Mock<INullJobWithSomeRequest>> _jobs = [];

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentRehydration = Limit };

    void Establish()
    {
        for (var i = 0; i < JobCount; i++)
        {
            var id = (JobId)Guid.NewGuid();
            var job = AddJob<INullJobWithSomeRequest>(id);

            // Newest first in storage so ordering oldest-first is observable.
            _storedJobs.Single(j => j.Id == id).Created = DateTimeOffset.UtcNow.AddMinutes(-i);
            job.Setup(_ => _.Resume()).Returns(async () =>
            {
                var now = Interlocked.Increment(ref _inFlight);
                int seen;
                while (now > (seen = Volatile.Read(ref _peak)) && Interlocked.CompareExchange(ref _peak, now, seen) != seen)
                {
                }

                lock (_resumeOrder)
                {
                    _resumeOrder.Add(id);
                }

                await Task.Delay(20);
                Interlocked.Decrement(ref _inFlight);
                return Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success);
            });
            _jobs.Add(job);
        }
    }

    async Task Because()
    {
        await _manager.Rehydrate();
        await WaitForRehydration();
    }

    [Fact] void should_never_exceed_the_limit() => (_peak <= Limit).ShouldBeTrue();
    [Fact] void should_run_jobs_concurrently() => (_peak > 1).ShouldBeTrue();
    [Fact] void should_resume_every_job_once() => _jobs.ForEach(job => job.Verify(_ => _.Resume(), Times.Once));
    [Fact] void should_start_with_the_oldest_job() => _resumeOrder.Take(Limit).Contains(_storedJobs.OrderBy(j => j.Created).First().Id).ShouldBeTrue();
}
