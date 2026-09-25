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

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The migrations create the state columns as the provider's native JSON type, and on PostgreSQL that
        // is jsonb, which refuses an implicit cast from text. Without this binding EF Core sends the string as
        // text and every write of a job or a step fails with "column is of type jsonb but expression is of type
        // text" - so a job can be started, reported as started, and never reach the database. No other provider
        // needs it: SQLite and SQL Server take the string as it is, which is why this is invisible until the
        // first PostgreSQL deployment.
        if (!Database.IsNpgsql())
        {
            return;
        }

        modelBuilder.Entity<Job>().Property(_ => _.StateJson).HasColumnType("jsonb");
        modelBuilder.Entity<JobStep>().Property(_ => _.StateJson).HasColumnType("jsonb");
    }
}
