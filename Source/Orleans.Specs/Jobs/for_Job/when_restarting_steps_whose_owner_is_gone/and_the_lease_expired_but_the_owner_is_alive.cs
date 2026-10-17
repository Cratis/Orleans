// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_the_lease_expired_but_the_owner_is_alive : given.a_job_with_leases
{
    void Establish()
    {
        StoredJobStep(_stepIds[0]).Lease = LeaseFor("silo-alive", TimeSpan.FromMinutes(10));
        _job.ActiveSiloAddresses = new HashSet<string> { "silo-alive" };
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_start_the_step_again() => StartsOf(0).ShouldEqual(1);
}
