// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Jobs;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

public class a_job_step_with_a_stored_parent : a_job_step_left_scheduled_by_a_dead_process
{
    protected readonly JobKey _parentKey = new("event-store", "namespace");
    protected readonly JobType _parentType = new("restarted-job");
    protected Mock<IJob> _parent;
    protected GrainId _parentId;
    protected for_Jobs.given.job_storage _jobStorage;
    protected IJobTypes _jobTypes;

    void Establish()
    {
        var jobsStorage = Substitute.For<IJobsStorage>();
        _jobStorage = Substitute.For<for_Jobs.given.job_storage>(Substitute.For<IJobStorage>());
        _jobTypes = Substitute.For<IJobTypes>();
        jobsStorage.GetFor(_parentKey.Scope, _parentKey.Namespace).Returns(new JobsStorage(_jobStorage, Substitute.For<IJobStepStorage>()));
        _jobStorage.GetJobType(_jobId).Returns(Task.FromResult<Catch<JobType, Storage.Jobs.JobError>>(_parentType));
        _jobTypes.GetClrTypeFor(_parentType).Returns(Result.Success<Type, IJobTypes.GetClrTypeForError>(typeof(IJob)));
        _silo.AddService(jobsStorage);
        _silo.AddService(_jobTypes);
        _parentId = GrainId.Create(GrainType.Create("restarted-parent"), GrainIdKeyExtensions.CreateGuidKey(_jobId, _parentKey));
        var context = Substitute.For<IGrainContext>();
        context.GrainId.Returns(_parentId);
        _parent = new();
        _parent.As<IGrainBase>().SetupGet(job => job.GrainContext).Returns(context);
        _parent.Setup(job => job.OnStepStopped(_jobStepId, It.IsAny<JobStepResult>())).ReturnsAsync(Result<JobError>.Success());
        _silo.AddProbe(_ => _parent.Object);
        _jobStep.JobResolver = id => id.Equals(_parentId) ? _parent.Object : _jobStep.ReportedJob;
    }

    void Destroy() => _jobStep.Dispose();
}
