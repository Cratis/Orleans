// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Jobs.Integration.Stages;

/// <summary>
/// A job that runs its items in stages - the steps of a stage in parallel, the stages one after the other.
/// </summary>
public class StagedIntegrationJob : Job<StagedIntegrationJobRequest, JobState>, IStagedIntegrationJob
{
    /// <inheritdoc/>
    protected override bool KeepAfterCompleted => true;

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(StagedIntegrationJobRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Stages
                .SelectMany((items, stage) => items.Select(item => CreateStep<IIntegrationJobStep>(item, (JobStepStage)stage)))
                .ToImmutableList());
}
