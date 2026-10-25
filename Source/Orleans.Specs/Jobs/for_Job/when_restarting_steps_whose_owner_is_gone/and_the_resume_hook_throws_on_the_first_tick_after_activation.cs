// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone;

public class and_the_resume_hook_throws_on_the_first_tick_after_activation : given.a_job_with_leases
{
    async Task Establish()
    {
        StoredJobStep(_stepIds[0]).Lease = LeaseFor("silo-gone", TimeSpan.FromMinutes(10));
        StoredJobStep(_stepIds[1]).Lease = LeaseFor("silo-alive", TimeSpan.FromSeconds(5));
        _job.ActiveSiloAddresses = new HashSet<string> { "silo-alive" };
        _job.CurrentState.Request = new for_Job.given.SomeRequest();
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        await _job.OnActivateAsync(CancellationToken.None);
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_attempt_the_resume_hook_again_then_back_off() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_not_restart_the_lease_expired_step() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
