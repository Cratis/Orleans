// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class and_its_stop_was_already_counted : given.the_job
{
    JobStepId _firstId;
    JobStepId _secondId;
    Result<JobError> _duplicate;
    Result<JobError> _pending;
    int _stopsAfterDuplicate;
    int _writesAfterDuplicate;

    async Task Establish()
    {
        _firstId = JobStepId.New();
        _secondId = JobStepId.New();
        foreach (var id in new[] { _firstId, _secondId })
        {
            AddJobStep(id).Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
        }
        await _job.Start(new());

        // Stop persists the job's status before its steps report their outcomes.
        _job.CurrentState.Status = JobStatus.Stopped;
        await _job.OnStepStopped(_firstId, JobStepResult.Succeeded());
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because()
    {
        _duplicate = await _job.OnStepStopped(_firstId, JobStepResult.Succeeded());
        _stopsAfterDuplicate = _job.CurrentState.Progress.StoppedSteps;
        _writesAfterDuplicate = _silo.StorageStats<given.SomeJob, given.SomeJobState>().Writes;
        _pending = await _job.OnStepStopped(_secondId, JobStepResult.Succeeded());
    }

    [Fact] void should_acknowledge_the_duplicate() => _duplicate.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_duplicate() => _stopsAfterDuplicate.ShouldEqual(1);
    [Fact] void should_not_write_for_the_duplicate() => _writesAfterDuplicate.ShouldEqual(0);
    [Fact] void should_accept_the_pending_stop() => _pending.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_each_step_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(2);
    [Fact] void should_deliver_each_result_once() => _job.HandledStepResults.ShouldContainOnly(_firstId, _secondId);
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
