// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_admission_fails_at_the_next_stage : given.a_job_with_three_stages
{
    for_Job.given.SomeAdmission _admission;
    JobStatus _statusWhileFailing;
    int _completedWhileFailing;

    void Establish()
    {
        _admission = for_Job.given.SomeAdmission.Admitting();
        Admit(_admission);
    }

    async Task Because()
    {
        await _job.Start(new());
        _admission.Answer = for_Job.given.SomeAdmission.Throwing().Answer;
        await Succeed(_first);
        _statusWhileFailing = _job.CurrentState.Status;
        _completedWhileFailing = _job.AllStepsCompletedCount;
        ShouldNotHaveStarted(_secondStepA);
        _admission.Answer = () => Task.FromResult(true);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_stay_running_while_it_cannot_be_answered() => _statusWhileFailing.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete_while_it_cannot_be_answered() => _completedWhileFailing.ShouldEqual(0);
    [Fact] void should_start_the_first_step_in_the_next_stage_once_admitted() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_start_the_second_step_in_the_next_stage_once_admitted() => ShouldHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_step_in_the_last_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_still_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
