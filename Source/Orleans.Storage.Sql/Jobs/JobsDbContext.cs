// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.Jobs;

/// <summary>
/// Represents the database context for jobs and job steps.
/// </summary>
/// <param name="options">The <see cref="DbContextOptions"/> to use.</param>
public class JobsDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for jobs.
    /// </summary>
    public DbSet<Job> Jobs { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for job steps.
    /// </summary>
    public DbSet<JobStep> JobSteps { get; set; }
}
