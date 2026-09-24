// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_getting_steps_for_a_job;

/// <summary>
/// Specifies that an unfiltered read returns every step, whatever its status.
/// </summary>
public class and_steps_have_mixed_statuses : job_steps_in_storage
{
    IImmutableList<JobStepState> _allSteps;
    int _runningCount;

    async Task Establish()
    {
        await SaveStep(JobStepStatus.Running);
        await SaveStep(JobStepStatus.Failed);
        await SaveStep(JobStepStatus.Stopped);
    }

    async Task Because()
    {
        _allSteps = (await _storage.GetForJob(_jobId)).AsT0;
        _runningCount = (await _storage.CountForJob(_jobId, JobStepStatus.Running)).AsT0;
    }

    [Fact] void should_return_every_step() => _allSteps.Count.ShouldEqual(3);

    [Fact] void should_count_only_the_matching_status() => _runningCount.ShouldEqual(1);

    Task SaveStep(JobStepStatus status)
    {
        var jobStepId = JobStepId.New();
        return _storage.Save(_jobId, jobStepId, AJobStepState(_jobId, jobStepId, status));
    }
}
