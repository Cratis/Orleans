// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_saving_a_step;

/// <summary>
/// A job resumes at the stage it had reached by reading the stage back from its steps, so the stage has to survive
/// the round trip through storage.
/// </summary>
public class that_runs_in_a_later_stage : job_steps_in_storage
{
    JobStepState _stored;

    async Task Because()
    {
        var jobStepId = JobStepId.New();
        var state = AJobStepState(_jobId, jobStepId, JobStepStatus.Unreachable);
        state.Stage = 20;
        await _storage.Save(_jobId, jobStepId, state);
        _stored = (await _storage.GetForJob(_jobId)).AsT0.Single();
    }

    [Fact] void should_read_the_stage_back() => _stored.Stage.ShouldEqual((JobStepStage)20);
    [Fact] void should_read_the_status_back() => _stored.Status.ShouldEqual(JobStepStatus.Unreachable);
}
