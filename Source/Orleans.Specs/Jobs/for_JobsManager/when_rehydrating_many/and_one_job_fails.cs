// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_rehydrating_many;

public class and_one_job_fails : given.the_manager
{
    Mock<INullJobWithSomeRequest> _failing;
    Mock<INullJobWithSomeRequest> _first;
    Mock<INullJobWithSomeRequest> _last;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentRehydration = 1 };

    void Establish()
    {
        _first = AddJob<INullJobWithSomeRequest>(Guid.NewGuid());
        _failing = AddJob<INullJobWithSomeRequest>(Guid.NewGuid());
        _last = AddJob<INullJobWithSomeRequest>(Guid.NewGuid());
        _storedJobs[0].Created = DateTimeOffset.UtcNow.AddMinutes(-3);
        _storedJobs[1].Created = DateTimeOffset.UtcNow.AddMinutes(-2);
        _storedJobs[2].Created = DateTimeOffset.UtcNow.AddMinutes(-1);
        _first.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        _failing.Setup(_ => _.Resume()).ThrowsAsync(new InvalidOperationException("boom"));
        _last.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    Task Because() => _manager.Rehydrate();

    [Fact] void should_attempt_the_failing_job_once() => _failing.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_resume_the_job_before_it() => _first.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_resume_the_job_after_it() => _last.Verify(_ => _.Resume(), Times.Once);
}
