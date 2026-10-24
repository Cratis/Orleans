// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Jobs.Integration.for_SqlJobStorage.given;

public abstract class a_sql_server_jobs_database : a_jobs_database
{
    protected override DbContextOptionsBuilder<JobsDbContext> Configure(DbContextOptionsBuilder<JobsDbContext> builder, string databaseName)
    {
        var connectionString = Environment.GetEnvironmentVariable("CRATIS_ORLEANS_SPECS_SQLSERVER")
            ?? throw new InvalidOperationException("Set CRATIS_ORLEANS_SPECS_SQLSERVER to a SQL Server connection string.");
        var connection = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
        return builder.UseSqlServer(connection.ConnectionString);
    }
}
