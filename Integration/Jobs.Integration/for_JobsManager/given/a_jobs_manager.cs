// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

/// <summary>
/// A jobs manager on the shared silo, for the scope and namespace the specs read their storage from.
/// </summary>
public class a_jobs_manager : all_dependencies
{
    /// <summary>
    /// Gets the jobs manager under specification.
    /// </summary>
    public IJobsManager JobsManager { get; private set; } = null!;

    void Establish()
    {
        JobsManager = Fixture.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty);
    }
}
