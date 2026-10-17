// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;
using Cratis.Orleans.Storage.Sql.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_getting_steps_for_a_job;

public class and_a_step_was_written_before_attempts_and_leases_existed : job_steps_in_storage
{
    JobStepState _stored;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.JobSteps.Add(new JobStep
        {
            Id = Guid.NewGuid(),
            JobId = _jobId.Value,
            JobStepId = Guid.NewGuid(),
            Type = "some-step-type",
            Name = "some step",
            Status = JobStepStatus.Running,
            IsPrepared = true,
            StateJson = "{\"IsPrepared\":true,\"Status\":2}"
        });
        await context.SaveChangesAsync();
    }

    async Task Because() => _stored = (await _storage.GetForJob(_jobId)).AsT0.Single();

    [Fact] void should_read_it_as_the_first_attempt() => _stored.Attempt.ShouldEqual(1);
    [Fact] void should_read_it_without_a_lease() => _stored.Lease.ShouldBeNull();
}
