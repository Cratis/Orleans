// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_with_a_throwing_resume_start : the_job
{
    protected List<JobStepId> _stepIds = [];
    protected List<Mock<ISomeJobStep>> _steps = [];

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 2 };

    protected Task Succeed(int index) => _job.OnStepSucceeded(_stepIds[index], JobStepResult.Succeeded());

    void Establish()
    {
        var key = new JobStepKey(_jobId, _jobKey.Scope, _jobKey.Namespace);
        var probes = new Dictionary<IdSpan, ISomeJobStep>();
        for (var index = 0; index < 3; index++)
        {
            var id = JobStepId.New();
            var identity = GrainIdKeyExtensions.CreateGuidKey(id, key);
            var step = CreateStepWithObserverReference(GrainId.Create(GrainType.Create("some-job-step"), identity));
            step.Setup(call => call.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
            _stepIds.Add(id);
            _steps.Add(step);
            probes[identity] = step.Object;
            StoredJobStep(id).Status = JobStepStatus.Running;
        }
        _silo.AddProbe<ISomeJobStep>(identity => probes[identity]);
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ThrowsAsync(new TimeoutException("Start timed out"));
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.Progress.TotalSteps = 3;
        CountStepsInStorage((JobStepStatus.Running, 3));
    }

    static Mock<ISomeJobStep> CreateStepWithObserverReference(GrainId id)
    {
        var step = new Mock<ISomeJobStep>();
        var context = Substitute.For<IGrainContext>();
        var runtime = Substitute.For<IGrainReferenceRuntime>();
        runtime.Cast(Arg.Any<IAddressable>(), typeof(IJobObserver)).Returns(call => call.Arg<IAddressable>());
        var shared = new GrainReferenceShared(id.Type, default, 0, runtime, default, null!, null!, null!);
        context.GrainReference.Returns(new Mock<GrainReference>(shared, id.Key).Object);
        context.GrainId.Returns(id);
        step.As<IGrainBase>().SetupGet(grain => grain.GrainContext).Returns(context);
        return step;
    }
}
