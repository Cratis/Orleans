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

    readonly Dictionary<IdSpan, ISomeJobStep> _notifiableSteps = [];

    /// <summary>
    /// Adds a step the job can notify, as notifying goes through the step's observer reference, which a plain probe cannot be cast to.
    /// </summary>
    /// <param name="stepId">The step to add.</param>
    /// <returns>The step.</returns>
    protected Mock<ISomeJobStep> AddNotifiableStep(JobStepId stepId)
    {
        var identity = GrainIdKeyExtensions.CreateGuidKey(stepId, new JobStepKey(_jobId, _jobKey.Scope, _jobKey.Namespace));
        var step = new Mock<ISomeJobStep>();
        var context = Substitute.For<IGrainContext>();
        var runtime = Substitute.For<IGrainReferenceRuntime>();
        runtime.Cast(Arg.Any<IAddressable>(), typeof(IJobObserver)).Returns(call => call.Arg<IAddressable>());
        var shared = new GrainReferenceShared(GrainType.Create("some-job-step"), default, 0, runtime, default, null!, null!, null!);
        var grainId = GrainId.Create(GrainType.Create("some-job-step"), identity);
        context.GrainReference.Returns(new Mock<GrainReference>(shared, identity).Object);
        context.GrainId.Returns(grainId);
        step.As<IGrainBase>().SetupGet(grain => grain.GrainContext).Returns(context);
        _notifiableSteps[identity] = step.Object;
        if (_notifiableSteps.Count == 1)
        {
            _silo.AddProbe<ISomeJobStep>(id => _notifiableSteps[id]);
        }
        return step;
    }

    void Establish()
    {
        _stepId = Guid.Parse("7d7d7d7d-0000-0000-0000-000000000001");
        _step = AddNotifiableStep(_stepId);
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
