// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class SomeJob : Job<SomeRequest, SomeJobState>, IGrainType
{
    public List<JobStepDetails> StepsToPrepare = [];
    public bool OnCompletedThrows;
    public bool ShouldBeRemovedAfterCompleted;
    public bool ShouldBeResumable;
    public HashSet<JobStepStage> StagesContinuingOnFailure = [];

    public Type GrainType => typeof(ISomeJob);

    public SomeJobState CurrentState => State;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(SomeRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(StepsToPrepare.ToImmutableList());

    protected override bool KeepAfterCompleted => !ShouldBeRemovedAfterCompleted;
    protected override Task OnAllStepsCompleted() => OnCompletedThrows
        ? Task.FromException(new Exception())
        : Task.CompletedTask;

    protected override Task<bool> CanResume() => Task.FromResult(ShouldBeResumable);

    protected override JobStageFailureBehavior GetFailureBehaviorFor(JobStepStage stage) =>
        StagesContinuingOnFailure.Contains(stage) ? JobStageFailureBehavior.ContinueWithNextStage : JobStageFailureBehavior.StopJob;
}