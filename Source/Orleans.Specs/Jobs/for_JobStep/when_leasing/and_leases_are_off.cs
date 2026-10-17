// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_leasing;

public class and_leases_are_off : given.a_job_step_left_scheduled_by_a_dead_process
{
    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.CurrentRun!.WorkerSettled.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_never_hold_a_lease() => _storage.Persisted.ShouldEachConformTo(_ => _.Lease is null);

    void Destroy() => _jobStep.Dispose();
}
