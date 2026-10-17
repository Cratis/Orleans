// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_stage_admission_timer_is_disposed_on_stop : given.a_job_with_three_stages
{
    int _timersBefore;

    for_Job.given.SomeAdmission _admission;

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
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        await _job.Stop();
    }

    [Fact] void should_have_been_waiting_to_ask_again() => _timersBefore.ShouldEqual(1);
    [Fact] void should_have_no_timer_left() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
