// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_reporting;

public class an_outcome_of_a_run_started_with_a_generation : given.a_job_step_left_scheduled_by_a_dead_process
{
    const string SentToStepsKey = "Cratis.Orleans.Jobs.Generation";
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    readonly TaskCompletionSource<(object? Sent, object? Reported)> _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    (object? Sent, object? Reported) _context;

    void Establish() =>
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult((RequestContext.Get(SentToStepsKey), RequestContext.Get(ReportedKey)));
            return Task.FromResult(Result<JobError>.Success());
        });

    async Task Because()
    {
        RequestContext.Set(SentToStepsKey, 3L);
        try
        {
            (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        }
        finally
        {
            RequestContext.Remove(SentToStepsKey);
        }
        _context = await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_report_the_generation_it_was_started_in() => _context.Reported.ShouldEqual(3L);
    [Fact] void should_not_pass_on_the_generation_its_job_sent() => _context.Sent.ShouldBeNull();

    async Task Destroy()
    {
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
