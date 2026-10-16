// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_has_already_completed : given.a_job_step_left_scheduled_by_a_dead_process
{
    async Task Establish()
    {
        await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.ReportedJob.ClearReceivedCalls();
    }

    async Task Because() => await _jobStep.OnJobStopped();

    [Fact] void should_preserve_success() => _persistedState.Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
    [Fact] void should_not_report_a_stop() => _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] void should_not_write_another_status() => _persistedState.StatusChanges.Count(change => change.Status is JobStepStatus.Stopped).ShouldEqual(0);

    void Destroy() => _jobStep.Dispose();
}
