// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_performing_work;

public class without_a_state_copier : given.a_job_step_left_scheduled_by_a_dead_process
{
    protected override bool RegisterStateCopier => false;

    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] async Task should_report_a_failure_naming_the_missing_copier() => await _jobStep.ReportedJob.Received(1).OnStepFailed(_jobStepId, Arg.Is<JobStepResult>(result => NamesTheCopier(result)));
    [Fact] void should_record_the_step_as_failed() => _persistedState.Status.ShouldEqual(JobStepStatus.CompletedWithFailure);

    void Destroy() => _jobStep.Dispose();

    static bool NamesTheCopier(JobStepResult result) => result.TryGetError(out var error) && error.ErrorMessages.Any(message => message.Contains("DeepCopier", StringComparison.Ordinal));
}
