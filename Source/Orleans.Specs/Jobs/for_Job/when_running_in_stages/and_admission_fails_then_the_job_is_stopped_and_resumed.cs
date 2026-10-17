// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_admission_fails_then_the_job_is_stopped_and_resumed : given.a_job_with_three_stages
{
    for_Job.given.SomeAdmission _admission;

    void Establish()
    {
        _admission = for_Job.given.SomeAdmission.Admitting();
        Admit(_admission);
        _job.ShouldBeResumable = true;
    }

    async Task Because()
    {
        await _job.Start(new());
        _admission.Answer = for_Job.given.SomeAdmission.Throwing().Answer;
        await Succeed(_first);
        _admission.Answer = () => Task.FromResult(true);
        await _job.Stop();
        await _job.Resume();
        await Succeed(_secondA);
        await Succeed(_secondB);
        await Succeed(_third);
    }

    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_run_the_completion_hook_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
