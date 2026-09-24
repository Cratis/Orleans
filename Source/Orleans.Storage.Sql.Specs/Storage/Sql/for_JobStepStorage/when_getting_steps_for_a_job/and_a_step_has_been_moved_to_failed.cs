// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_getting_steps_for_a_job;

/// <summary>
/// Specifies that a failed step is still readable, rather than disappearing.
/// </summary>
/// <remarks>
/// A failed step is the one a caller most needs to see afterwards - it carries why the job did not
/// finish. Moving a step to failed must therefore keep it retrievable by an unfiltered read, and by a
/// read filtered to the failed status. Losing it here looks exactly like a job that never had steps.
/// </remarks>
public class and_a_step_has_been_moved_to_failed : job_steps_in_storage
{
    JobStepId _jobStepId;
    IImmutableList<JobStepState> _allSteps;
    IImmutableList<JobStepState> _failedSteps;

    async Task Establish()
    {
        _jobStepId = JobStepId.New();
        await _storage.Save(_jobId, _jobStepId, AJobStepState(_jobId, _jobStepId, JobStepStatus.Running));
        await _storage.MoveToFailed(_jobId, _jobStepId, AJobStepState(_jobId, _jobStepId, JobStepStatus.Failed));
    }

    async Task Because()
    {
        _allSteps = (await _storage.GetForJob(_jobId)).AsT0;
        _failedSteps = (await _storage.GetForJob(_jobId, JobStepStatus.Failed)).AsT0;
    }

    [Fact] void should_still_return_the_step_when_unfiltered() => _allSteps.Count.ShouldEqual(1);

    [Fact] void should_report_the_step_as_failed() => _allSteps[0].Status.ShouldEqual(JobStepStatus.Failed);

    [Fact] void should_return_the_step_when_filtering_on_failed() => _failedSteps.Count.ShouldEqual(1);

    [Fact] void should_not_duplicate_the_step() => _allSteps.Select(_ => _.Id.JobStepId).Distinct().Count().ShouldEqual(1);
}
