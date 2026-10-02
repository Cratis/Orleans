// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_cleaning_up_many;

public class with_many_dead_jobs : given.the_manager
{
    const int JobCount = 15;
    const int Limit = 2;

    int _inFlight;
    int _peak;
    readonly List<Mock<INullJobWithSomeRequest>> _jobs = [];

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentCleanup = Limit };

    void Establish()
    {
        for (var i = 0; i < JobCount; i++)
        {
            var id = (JobId)Guid.NewGuid();
            var job = AddJob<INullJobWithSomeRequest>(id);
            var state = _storedJobs.Single(j => j.Id == id);
            state.Status = JobStatus.PreparingJob;
            state.Created = DateTimeOffset.UtcNow.AddHours(-3);
            _jobStepStorage.CountForJob(id, Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch.Success(0)));
            job.Setup(_ => _.Remove()).Returns(async () =>
            {
                var now = Interlocked.Increment(ref _inFlight);
                int seen;
                while (now > (seen = Volatile.Read(ref _peak)) && Interlocked.CompareExchange(ref _peak, now, seen) != seen)
                {
                }

                await Task.Delay(20);
                Interlocked.Decrement(ref _inFlight);
                return Result.Success<RemoveJobError>();
            });
            _jobs.Add(job);
        }
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] void should_never_exceed_the_limit() => (_peak <= Limit).ShouldBeTrue();
    [Fact] void should_delete_every_dead_job() => _jobs.ForEach(job => job.Verify(_ => _.Remove(), Times.Once));
}
