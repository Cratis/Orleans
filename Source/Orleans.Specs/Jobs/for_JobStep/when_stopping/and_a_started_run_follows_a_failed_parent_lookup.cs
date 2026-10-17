// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_a_started_run_follows_a_failed_parent_lookup : given.a_job_step_with_a_stored_parent
{
    Result<StartJobStepError> _started;

    async Task Establish()
    {
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
        (await _jobStep.Stop(false)).AsT1.ShouldEqual(JobStepError.FailedToReportToJob);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.Work = async (step, _) => await step.PerformCancelled.Task;
    }

    async Task Because()
    {
        _started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_allow_starting_after_the_failed_lookup() => _started.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_not_look_up_a_start_supplied_parent() => await _jobStorage.Received(1).GetJobType(_jobId);
    [Fact] async Task should_report_to_the_start_supplied_parent() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_not_report_to_the_unresolved_parent() => _parent.Verify(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>()), Times.Never);
}
