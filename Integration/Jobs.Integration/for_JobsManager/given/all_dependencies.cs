// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

/// <summary>
/// The storage and the step processor every job-system scenario reaches for, against the shared silo.
/// </summary>
/// <remarks>
/// The scenarios read what the job system persisted rather than what a client API reports, so a step that
/// is written but cannot be read back fails here instead of looking like a missing step.
/// </remarks>
public class all_dependencies : Specification
{
    /// <summary>
    /// Gets the silo the specs run against.
    /// </summary>
    public JobsClusterFixture Fixture { get; private set; } = null!;

    /// <summary>
    /// Gets the processor the steps report their progress to.
    /// </summary>
    public TheJobStepProcessor JobStepProcessor { get; private set; } = null!;

    /// <summary>
    /// Gets the storage the job system persists jobs to.
    /// </summary>
    public IJobStorage JobStorage { get; private set; } = null!;

    /// <summary>
    /// Gets the storage the job system persists job steps to.
    /// </summary>
    public IJobStepStorage JobStepStorage { get; private set; } = null!;

    void Establish()
    {
        Fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();

        JobStepProcessor = Fixture.Services.GetRequiredService<TheJobStepProcessor>();
        JobStepProcessor.Reset();

        var storage = Fixture.Services.GetRequiredService<IJobsStorage>().GetFor(JobsClusterFixture.Scope, string.Empty);
        JobStorage = storage.Jobs;
        JobStepStorage = storage.JobSteps;
    }
}
