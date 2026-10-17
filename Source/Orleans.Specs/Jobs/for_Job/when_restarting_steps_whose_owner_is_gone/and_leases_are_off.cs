// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_leases_are_off : for_Job.given.a_running_job_with_steps
{
    void Establish()
    {
        CountStepsInStorage((JobStepStatus.Running, 2));
        StoredJobStep(_stepIds[0]).Lease = new() { Owner = "silo-gone", LastHeartbeat = DateTimeOffset.UtcNow.AddDays(-1) };
        _job.ActiveSiloAddresses = new HashSet<string>();
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_never_restart_a_step() => StartsOf(0).ShouldEqual(1);
}
