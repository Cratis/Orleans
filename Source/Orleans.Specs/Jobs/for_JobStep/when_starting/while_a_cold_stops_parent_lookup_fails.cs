// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class while_a_cold_stops_parent_lookup_fails : given.a_job_step_with_a_stored_parent
{
    readonly TaskCompletionSource<Catch<JobType, Storage.Jobs.JobError>> _lookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _succeeded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<JobStepError> _stopped;
    Result<StartJobStepError> _started;
    int _workBeforeLookupSettled;

    void Establish()
    {
        _jobStorage.GetJobType(_jobId).Returns(_lookup.Task);
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _succeeded.TrySetResult();
            return Task.FromResult(Result<JobError>.Success());
        });
    }

    async Task Because()
    {
        Task<Result<JobStepError>> stopping;
        using (JobExecutionGeneration.Carry(1))
        {
            stopping = _jobStep.Stop(false);
        }
        using (JobExecutionGeneration.Carry(2))
        {
            _started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        }
        _workBeforeLookupSettled = _jobStep.WorkRuns;
        _lookup.TrySetResult(Storage.Jobs.JobError.NotFound);
        _stopped = await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
        await _succeeded.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_report_the_failed_lookup() => _stopped.AsT1.ShouldEqual(JobStepError.FailedToReportToJob);
    [Fact] void should_accept_the_deferred_start() => _started.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_run_during_the_lookup() => _workBeforeLookupSettled.ShouldEqual(0);
    [Fact] void should_run_once_the_failed_lookup_settles() => _jobStep.WorkRuns.ShouldEqual(1);
    [Fact] async Task should_report_the_new_runs_success() => await _jobStep.ReportedJob.Received(1).OnStepSucceeded(_jobStepId, Arg.Any<JobStepResult>());

    void Destroy() => _lookup.TrySetResult(Storage.Jobs.JobError.NotFound);
}
