// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_background_start_is_pending : given.the_job
{
    given.SomeAdmission _admission;
    Cratis.Monads.Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Establish()
    {
        _job.StartInBackground = true;
        (await _job.Start(new())).IsSuccess.ShouldBeTrue();
        _admission = given.SomeAdmission.Refusing();
        Admit(_admission);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_the_pending_start_as_running() => _result.AsT0.ShouldEqual(ResumeJobSuccess.JobAlreadyRunning);
    [Fact] void should_not_ask_admission_again() => _admission.Asked.ShouldBeEmpty();
}
