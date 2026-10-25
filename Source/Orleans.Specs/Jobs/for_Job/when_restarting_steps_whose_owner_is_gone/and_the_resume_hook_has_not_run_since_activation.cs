// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_the_resume_hook_has_not_run_since_activation : given.a_job_with_leases
{
    async Task Establish()
    {
        StoredJobStep(_stepIds[0]).Lease = LeaseFor("silo-gone", TimeSpan.FromMinutes(10));
        StoredJobStep(_stepIds[1]).Lease = LeaseFor("silo-alive", TimeSpan.FromSeconds(5));
        _job.ActiveSiloAddresses = new HashSet<string> { "silo-alive" };
        _job.CurrentState.Request = new SomeRequest();
        _job.BeforeResuming = () => Task.CompletedTask;
        await _job.OnActivateAsync(CancellationToken.None);
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_run_the_resume_hook_once() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_resume_the_steps_from_storage() => (StartsOf(0) > 1).ShouldBeTrue();
    [Fact] void should_not_restart_the_lease_expired_step_a_second_time_in_the_tick() => StartsOf(0).ShouldEqual(2);
}
