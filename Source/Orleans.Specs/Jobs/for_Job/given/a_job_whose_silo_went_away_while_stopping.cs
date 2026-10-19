// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job persisted as stopped by a silo that went away before its step reported, as a fresh activation finds it.
/// </summary>
public class a_job_whose_silo_went_away_while_stopping : the_job
{
    protected JobStepId _stepId;
    protected Mock<ISomeJobStep> _step;

    protected int Deactivations => _silo.GrainRuntime.Mock.Invocations.Count(invocation => invocation.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));

    void Establish()
    {
        _stepId = Guid.Parse("7d7d7d7d-0000-0000-0000-000000000001");
        var identity = GrainIdKeyExtensions.CreateGuidKey(_stepId, new JobStepKey(_jobId, _jobKey.Scope, _jobKey.Namespace));

        // Notifying a step goes through its observer reference, which a plain probe cannot be cast to.
        _step = new Mock<ISomeJobStep>();
        var context = Substitute.For<IGrainContext>();
        var runtime = Substitute.For<IGrainReferenceRuntime>();
        runtime.Cast(Arg.Any<IAddressable>(), typeof(IJobObserver)).Returns(call => call.Arg<IAddressable>());
        var shared = new GrainReferenceShared(GrainType.Create("some-job-step"), default, 0, runtime, default, null!, null!, null!);
        context.GrainReference.Returns(new Mock<GrainReference>(shared, identity).Object);
        context.GrainId.Returns(GrainId.Create(GrainType.Create("some-job-step"), identity));
        _step.As<IGrainBase>().SetupGet(grain => grain.GrainContext).Returns(context);
        _silo.AddProbe<ISomeJobStep>(_ => _step.Object);
        StoredJobStep(_stepId).Status = JobStepStatus.Stopped;
        _job.CurrentState.Request = new SomeRequest();
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.PreparingJob });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Progress.TotalSteps = 1;
    }
}
