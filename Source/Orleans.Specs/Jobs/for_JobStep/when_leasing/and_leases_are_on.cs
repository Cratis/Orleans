// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_leasing;

public class and_leases_are_on : given.a_job_step_left_scheduled_by_a_dead_process
{
    protected override JobsOptions CreateOptions() => new() { StepCheckpointBatchInterval = 1, StepLeaseTimeout = TimeSpan.FromMinutes(1) };

    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.CurrentRun!.WorkerSettled.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_hold_a_lease_while_scheduled() => _storage.Persisted.First(_ => _.Status == JobStepStatus.Scheduled).Lease.ShouldNotBeNull();
    [Fact] void should_hold_a_lease_while_running() => _storage.Persisted.First(_ => _.Status == JobStepStatus.Running).Lease.ShouldNotBeNull();
    [Fact] void should_beat_the_heart_when_it_takes_the_lease() => (DateTimeOffset.UtcNow - _storage.Persisted.First(_ => _.Status == JobStepStatus.Running).Lease!.LastHeartbeat).ShouldBeLessThan(TimeSpan.FromMinutes(1));
    [Fact] void should_name_the_attempt_in_the_lease() => _storage.Persisted.First(_ => _.Status == JobStepStatus.Running).Lease!.Attempt.ShouldEqual(1);
    [Fact] void should_identify_the_activation() => _storage.Persisted.First(_ => _.Status == JobStepStatus.Running).Lease!.ActivationId.ShouldNotEqual(Guid.Empty);
    [Fact] void should_give_the_lease_up_when_the_step_completes() => _storage.Persisted[^1].Lease.ShouldBeNull();

    void Destroy() => _jobStep.Dispose();
}
