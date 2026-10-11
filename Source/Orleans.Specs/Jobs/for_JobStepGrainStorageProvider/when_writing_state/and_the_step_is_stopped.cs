// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepGrainStorageProvider.when_writing_state;

public class and_the_step_is_stopped : given.a_job_step_storage_provider
{
    void Establish()
    {
        _state.StatusChanges.Add(new() { Status = JobStepStatus.Running });
        _state.StatusChanges.Add(new() { Status = JobStepStatus.Stopped });
    }

    Task Because() => _provider.WriteStateAsync(nameof(JobStepState), _grainId, _grainState);

    [Fact] Task should_save_the_state() => _jobStepStorage.Received(1).Save(_jobId, _jobStepId, _state);
    [Fact] Task should_not_move_the_state_to_failed() => _jobStepStorage.DidNotReceive().MoveToFailed(Arg.Any<JobId>(), Arg.Any<JobStepId>(), Arg.Any<JobStepState>());
    [Fact] Task should_not_remove_the_state() => _jobStepStorage.DidNotReceive().Remove(Arg.Any<JobId>(), Arg.Any<JobStepId>());
}
