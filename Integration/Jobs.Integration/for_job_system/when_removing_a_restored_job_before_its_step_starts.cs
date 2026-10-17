// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_removing_a_restored_job_before_its_step_starts : given.a_job_restored_before_its_step_starts
{
    Catch<JobState, Storage.Jobs.JobError> _job;
    IEnumerable<JobStepState> _steps;

    async Task Because()
    {
        (await _jobs.Delete(_key, _jobId).WaitAsync(Deadline, TimeProvider.System)).ShouldBeOfExactType<DeleteJobResult.Deleted>();
        await _signals.Stopped.Task.WaitAsync(Deadline, TimeProvider.System);
        await _signals.Completed.Task.WaitAsync(Deadline, TimeProvider.System);
        await _signals.StepDeactivating.Task.WaitAsync(Deadline, TimeProvider.System);
        _job = await _storage.Jobs.GetJob(_jobId);
        _steps = (await _storage.JobSteps.GetForJob(_jobId)).AsT0;
    }

    [Fact] void should_run_on_stopped() => _signals.Stopped.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_run_on_all_steps_completed() => _signals.Completed.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_clear_the_job() => _job.AsT1.ShouldEqual(Storage.Jobs.JobError.NotFound);
    [Fact] void should_clear_its_steps() => _steps.ShouldBeEmpty();
    [Fact] void should_not_start_background_work() => _signals.WorkEntered.Task.IsCompleted.ShouldBeFalse();
}
