// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

/// <summary>
/// A refusal that could not be made durable is not a stop: the job stays running and asks again like after a failure
/// to answer, and stops once the refusal can be written.
/// </summary>
public class and_the_refusal_cannot_be_written : given.a_job_with_three_stages
{
    for_Job.given.SomeAdmission _admission;
    JobStatus _statusWhileUnwritable;

    void Establish()
    {
        _admission = for_Job.given.SomeAdmission.Admitting();
        Admit(_admission);
    }

    async Task Because()
    {
        await _job.Start(new());
        _admission.Answer = () =>
        {
            _job.FailWrites = true;
            return Task.FromResult(false);
        };
        await Succeed(_first);
        _statusWhileUnwritable = _job.CurrentState.Status;
        _job.FailWrites = false;
        _admission.Answer = for_Job.given.SomeAdmission.Refusing().Answer;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_stay_running_while_the_refusal_cannot_be_written() => _statusWhileUnwritable.ShouldEqual(JobStatus.Running);
    [Fact] void should_stop_once_it_can_be_written() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_never_start_the_next_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
}
