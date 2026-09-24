// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

/// <summary>
/// A migrated jobs database with a <see cref="Sql.Jobs.JobStepStorage"/> over it.
/// </summary>
public class job_steps_in_storage : a_sqlite_jobs_database
{
    protected Sql.Jobs.JobStepStorage _storage;
    protected JobId _jobId;

    void Establish()
    {
        using (var context = CreateContext())
        {
            context.Database.Migrate();
        }

        _storage = new Sql.Jobs.JobStepStorage(CreateContext);
        _jobId = JobId.New();
    }

    protected static JobStepState AJobStepState(JobId jobId, JobStepId jobStepId, JobStepStatus status) =>
        new()
        {
            Id = new JobStepIdentifier(jobId, jobStepId),
            Type = new JobStepType("some-step-type"),
            Name = (JobStepName)"some step",
            Status = status,
            IsPrepared = true
        };
}
