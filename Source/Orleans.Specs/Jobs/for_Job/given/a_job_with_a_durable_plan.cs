// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_with_a_durable_plan : the_job
{
    protected JobStepId _firstId;
    protected JobStepId _secondId;
    protected JobStepId _laterId;
    protected Mock<ISomeJobStep> _first;
    protected Mock<ISomeJobStep> _second;
    protected Mock<ISomeJobStep> _later;
    protected Result<ResumeJobSuccess, ResumeJobError> _result;
    protected List<JobStepId> _dispatched;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1 };

    protected async Task ActivateFrom(JobStatus status)
    {
        var snapshot = _job.WrittenStates.Last(state => state.Status == status);
        _silo = new();
        ConfigureSilo();
        _silo.StorageManager.GetStorage<SomeJobState>(typeof(SomeJob).FullName!).State = snapshot;
        AddSteps();
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
    }

    protected void InterruptPreparation() => _second.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<Stages.JobStepStage>()))
        .ThrowsAsync(new InvalidOperationException("The silo stopped preparing"));

    void AddSteps()
    {
        _first = AddJobStep(_firstId);
        _second = AddJobStep(_secondId);
        _later = AddJobStep(_laterId, 10);
        _dispatched = [];
        foreach (var (id, step) in new[] { (_firstId, _first), (_secondId, _second), (_laterId, _later) })
        {
            step.Setup(grain => grain.Start(It.IsAny<GrainId>())).Returns(() =>
            {
                _job.DispatchContexts.Count.ShouldEqual(1);
                _dispatched.Add(id);

                // TestKit probes are not Orleans references; AlreadyStarted skips the start path's reference cast.
                return Task.FromResult(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
            });
        }
    }

    void Establish()
    {
        _firstId = JobStepId.New();
        _secondId = JobStepId.New();
        _laterId = JobStepId.New();
        AddSteps();
    }
}
