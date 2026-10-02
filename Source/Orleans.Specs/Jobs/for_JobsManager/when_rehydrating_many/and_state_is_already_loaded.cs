// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_rehydrating_many;

public class and_state_is_already_loaded : given.the_manager
{
    Mock<INullJobWithSomeRequest> _job;

    void Establish()
    {
        _job = AddJob<INullJobWithSomeRequest>(Guid.NewGuid());
        _job.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    Task Because() => _manager.Rehydrate();

    [Fact] void should_resume_the_job() => _job.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_not_read_the_job_again() => _jobStorage.DidNotReceive().GetJob(Arg.Any<JobId>());
}
