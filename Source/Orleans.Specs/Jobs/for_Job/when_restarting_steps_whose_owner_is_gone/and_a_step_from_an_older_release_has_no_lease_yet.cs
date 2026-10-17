// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_a_step_from_an_older_release_has_no_lease_yet : given.a_job_with_leases
{
    void Establish()
    {
        ChangedAgo(0, TimeSpan.FromSeconds(30));
        _job.ActiveSiloAddresses = new HashSet<string>();
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_leave_it_alone_for_one_lease_period() => StartsOf(0).ShouldEqual(1);
}
