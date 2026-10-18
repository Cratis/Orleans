// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_old_preparation_with_saved_plans : given.the_manager
{
    Mock<INullJobWithSomeRequest> _plannedJob;
    Mock<INullJobWithSomeRequest> _emptyPlannedJob;

    void Establish()
    {
        _plannedJob = AddRecoverableJob([new(JobStepId.New(), typeof(INullJobWithSomeRequest), 0, typeof(object).AssemblyQualifiedName!, "{}")]);
        _emptyPlannedJob = AddRecoverableJob([]);
        _jobStepStorage.CountForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch<int>.Success(0)));
    }

    async Task Because()
    {
        await _manager.Rehydrate();
        await WaitForRehydration();
    }

    [Fact] void should_recover_the_saved_plan() => _plannedJob.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_recover_an_empty_saved_plan() => _emptyPlannedJob.Verify(job => job.Resume(), Times.Once);
    [Fact] void should_not_remove_the_saved_plan() => _plannedJob.Verify(job => job.Remove(), Times.Never);
    [Fact] void should_not_remove_an_empty_saved_plan() => _emptyPlannedJob.Verify(job => job.Remove(), Times.Never);
    [Fact] void should_not_remove_job_storage() => _jobStorage.DidNotReceive().Remove(Arg.Any<JobId>());

    Mock<INullJobWithSomeRequest> AddRecoverableJob(IReadOnlyList<PlannedJobStep> plan)
    {
        var id = JobId.New();
        var job = AddJob<INullJobWithSomeRequest>(id);
        var state = _storedJobs.Single(state => state.Id == id);
        state.Status = JobStatus.PreparingSteps;
        state.Created = DateTimeOffset.UtcNow.AddHours(-2);
        state.StepPlan = plan;
        job.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
        return job;
    }
}
