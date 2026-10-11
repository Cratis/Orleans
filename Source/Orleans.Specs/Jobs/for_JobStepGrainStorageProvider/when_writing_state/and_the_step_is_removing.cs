// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepGrainStorageProvider.when_writing_state;

public class and_the_step_is_removing : given.a_job_step_storage_provider
{
    void Establish()
    {
        _state.StatusChanges.Add(new() { Status = JobStepStatus.Failed });
        _state.StatusChanges.Add(new() { Status = JobStepStatus.Removing });
    }

    Task Because() => _provider.WriteStateAsync(nameof(JobStepState), _grainId, _grainState);

    [Fact] Task should_remove_the_state() => _jobStepStorage.Received(1).Remove(_jobId, _jobStepId);
    [Fact] Task should_not_save_the_state() => _jobStepStorage.DidNotReceive().Save(Arg.Any<JobId>(), Arg.Any<JobStepId>(), Arg.Any<JobStepState>());
    [Fact] Task should_not_move_the_state_to_failed() => _jobStepStorage.DidNotReceive().MoveToFailed(Arg.Any<JobId>(), Arg.Any<JobStepId>(), Arg.Any<JobStepState>());
}
