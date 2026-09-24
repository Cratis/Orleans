// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// A job that plans one step per item in the request.
/// </summary>
public class IntegrationJob : Job<IntegrationJobRequest, JobState>, IIntegrationJob
{
    /// <summary>
    /// Gets whether the job stays queryable after completing - the engine removes completed jobs by default.
    /// </summary>
    protected override bool KeepAfterCompleted => true;

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(IntegrationJobRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Items.Select(item => new JobStepDetails(
                Type: typeof(IIntegrationJobStep),
                Id: JobStepId.New(),
                Key: new JobStepKey(JobId, Scope: string.Empty, Namespace: string.Empty),
                Request: item,
                ResultType: typeof(string))).ToImmutableList());
}
