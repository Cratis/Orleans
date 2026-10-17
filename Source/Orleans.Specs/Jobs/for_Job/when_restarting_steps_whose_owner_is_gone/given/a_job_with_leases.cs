// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_Job.when_restarting_steps_whose_owner_is_gone.given;

public class a_job_with_leases : for_Job.given.a_running_job_with_steps
{
    protected static readonly TimeSpan LeaseTimeout = TimeSpan.FromMinutes(1);

    protected override JobsOptions CreateOptions() => new() { StepLeaseTimeout = LeaseTimeout };

    protected static JobStepLease LeaseFor(string owner, TimeSpan age) =>
        new() { Owner = owner, ActivationId = Guid.NewGuid(), LastHeartbeat = DateTimeOffset.UtcNow - age };

    protected void ChangedAgo(int index, TimeSpan age) =>
        StoredJobStep(_stepIds[index]).StatusChanges = [new() { Status = JobStepStatus.Running, Occurred = DateTimeOffset.UtcNow - age }];

    void Establish() => CountStepsInStorage((JobStepStatus.Running, 2));
}
