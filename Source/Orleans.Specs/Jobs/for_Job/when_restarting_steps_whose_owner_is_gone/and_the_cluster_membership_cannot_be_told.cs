// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_the_cluster_membership_cannot_be_told : given.a_job_with_leases
{
    void Establish()
    {
        StoredJobStep(_stepIds[0]).Lease = LeaseFor("silo-gone", TimeSpan.FromMinutes(10));
        _job.ActiveSiloAddresses = null;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_start_the_step_again() => StartsOf(0).ShouldEqual(1);
}
