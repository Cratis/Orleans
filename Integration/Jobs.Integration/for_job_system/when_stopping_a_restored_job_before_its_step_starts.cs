// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_stopping_a_restored_job_before_its_step_starts : given.a_job_restored_before_its_step_starts
{
    JobState _job;
    JobStepState _step;

    async Task Because()
    {
        (await _jobs.Stop(_key, _jobId).WaitAsync(Deadline, TimeProvider.System)).ShouldBeOfExactType<StopJobResult.Stopped>();
        await _signals.Stopped.Task.WaitAsync(Deadline, TimeProvider.System);
        await _signals.StepDeactivating.Task.WaitAsync(Deadline, TimeProvider.System);
        _job = (await _storage.Jobs.GetJob(_jobId)).AsT0;
        _step = (await _storage.JobSteps.GetForJob(_jobId)).AsT0.Single();
    }

    [Fact] void should_count_the_step_stop() => _job.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_finish_stopping_the_job() => _job.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_run_on_stopped() => _signals.Stopped.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_keep_the_job_stopped() => _job.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_preserve_the_stopped_step_for_resume() => _step.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_start_background_work() => _signals.WorkEntered.Task.IsCompleted.ShouldBeFalse();
}
