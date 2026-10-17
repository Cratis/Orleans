// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

[Collection(JobsClusterCollection.Name)]
public class when_removing_a_restored_step_whose_job_is_gone : given.a_job_restored_before_its_step_starts
{
    IEnumerable<JobStepState> _steps;

    async Task Establish() => (await _storage.Jobs.Remove(_jobId)).RethrowError();

    async Task Because()
    {
        var step = JobsClusterFixture.Shared.GrainFactory.GetGrain<IIntegrationJobStep>(
            _stepId, keyExtension: new JobStepKey(_jobId, _key.Scope, _key.Namespace), grainClassNamePrefix: null);
        (await step.Stop(true).WaitAsync(Deadline, TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _signals.StepDeactivating.Task.WaitAsync(Deadline, TimeProvider.System);
        _steps = (await _storage.JobSteps.GetForJob(_jobId)).AsT0;
    }

    [Fact] void should_delete_the_orphaned_step() => _steps.ShouldBeEmpty();
    [Fact] void should_not_deliver_a_stop_to_a_missing_job() => _signals.Stopped.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_complete_a_missing_job() => _signals.Completed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_start_work() => _signals.WorkEntered.Task.IsCompleted.ShouldBeFalse();
}
