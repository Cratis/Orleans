// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_stopped_job_rejecting_resume : the_job
{
    protected Mock<ISomeJobStep> _step;
    protected Result<ResumeJobSuccess, ResumeJobError> _result;
    protected int _writesBefore;
    protected int _changesBefore;

    protected async Task ActivateFrom(SomeJobState state)
    {
        _silo = new();
        ConfigureSilo();
        var key = new JobStepKey(_jobId, _jobKey.Scope, _jobKey.Namespace);
        _step = _silo.AddProbe<ISomeJobStep>(_storedJobStepStates.Single().Id.JobStepId, keyExtension: key);
        _step.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        _silo.StorageManager.GetStorage<SomeJobState>(typeof(SomeJob).FullName!).State = state;
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
        _job.ShouldBeResumable = true;
    }

    void Establish()
    {
        var id = JobStepId.New();
        _step = AddJobStep(id);
        _step.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        StoredJobStep(id).Status = JobStepStatus.Stopped;
        _job.CurrentState.Request = new SomeRequest();
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.PreparingJob });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.StoppedSteps = 1;
        _job.ShouldBeResumable = true;
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        _writesBefore = Writes;
        _changesBefore = _job.CurrentState.StatusChanges.Count;
    }
}
