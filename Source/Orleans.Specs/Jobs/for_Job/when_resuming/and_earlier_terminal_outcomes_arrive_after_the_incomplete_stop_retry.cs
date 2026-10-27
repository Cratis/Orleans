// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_earlier_terminal_outcomes_arrive_after_the_incomplete_stop_retry : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    protected override int NumberOfSteps => 4;

    void Establish() => AllowResume();

    async Task Because()
    {
        await _job.Resume();
        _storedJobStepStates.Remove(StoredJobStep(_stepIds[1]));
        for (var delivery = 0; delivery < 2; delivery++)
        {
            RequestContext.Set(ReportedGenerationKey, _earlierGeneration);
            try
            {
                await Succeed(1);
            }
            finally
            {
                RequestContext.Remove(ReportedGenerationKey);
            }
            RequestContext.Set(ReportedGenerationKey, _earlierGeneration);
            try
            {
                await Fail(2);
            }
            finally
            {
                RequestContext.Remove(ReportedGenerationKey);
            }
        }
    }

    [Fact] void should_count_the_old_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_count_the_old_failure_once() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_handle_the_old_success_once() => _job.HandledStepResults.Count(id => id == _stepIds[1]).ShouldEqual(1);
    [Fact] void should_handle_the_old_failure_once() => _job.HandledStepResults.Count(id => id == _stepIds[2]).ShouldEqual(1);
    [Fact] void should_record_the_success_once() => _job.CurrentState.CompletedStepsCounted.Count(id => id == _stepIds[1]).ShouldEqual(1);
    [Fact] void should_record_the_failure_once() => _job.CurrentState.CompletedStepsCounted.Count(id => id == _stepIds[2]).ShouldEqual(1);
    [Fact] void should_keep_running_with_an_unfinished_sentinel() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
