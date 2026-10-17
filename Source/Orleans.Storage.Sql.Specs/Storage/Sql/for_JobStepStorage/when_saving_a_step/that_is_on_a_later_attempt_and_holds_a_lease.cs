// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_saving_a_step;

public class that_is_on_a_later_attempt_and_holds_a_lease : job_steps_in_storage
{
    JobStepState _stored;
    DateTimeOffset _heartbeat;
    Guid _activation;

    async Task Because()
    {
        var jobStepId = JobStepId.New();
        _heartbeat = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        _activation = Guid.NewGuid();
        var state = AJobStepState(_jobId, jobStepId, JobStepStatus.Running);
        state.Attempt = 3;
        state.Lease = new() { Owner = "silo-1", ActivationId = _activation, LastHeartbeat = _heartbeat, Attempt = 3 };
        await _storage.Save(_jobId, jobStepId, state);
        _stored = (await _storage.GetForJob(_jobId)).AsT0.Single();
    }

    [Fact] void should_read_the_attempt_back() => _stored.Attempt.ShouldEqual(3);
    [Fact] void should_read_the_lease_owner_back() => _stored.Lease!.Owner.ShouldEqual("silo-1");
    [Fact] void should_read_the_lease_activation_back() => _stored.Lease!.ActivationId.ShouldEqual(_activation);
    [Fact] void should_read_the_last_heartbeat_back() => _stored.Lease!.LastHeartbeat.ShouldEqual(_heartbeat);
}
