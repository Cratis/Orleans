// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Orleans.Jobs.Integration.for_SqlJobStorage.given;

public abstract class a_postgresql_jobs_database : a_jobs_database
{
    protected override DbContextOptionsBuilder<JobsDbContext> Configure(DbContextOptionsBuilder<JobsDbContext> builder, string databaseName)
    {
        var connectionString = Environment.GetEnvironmentVariable("CRATIS_ORLEANS_SPECS_POSTGRESQL")
            ?? throw new InvalidOperationException("Set CRATIS_ORLEANS_SPECS_POSTGRESQL to a PostgreSQL server connection string.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName };
        return builder.UseNpgsql(connection.ConnectionString);
    }
}
