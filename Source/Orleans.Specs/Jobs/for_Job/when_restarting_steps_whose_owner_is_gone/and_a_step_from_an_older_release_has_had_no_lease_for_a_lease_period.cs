// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_a_step_from_an_older_release_has_had_no_lease_for_a_lease_period : given.a_job_with_leases
{
    void Establish()
    {
        ChangedAgo(0, TimeSpan.FromMinutes(5));
        ChangedAgo(1, TimeSpan.FromSeconds(30));
        _job.ActiveSiloAddresses = new HashSet<string>();
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_start_it_again() => StartsOf(0).ShouldEqual(2);
    [Fact] void should_leave_the_step_that_changed_recently() => StartsOf(1).ShouldEqual(1);
}
