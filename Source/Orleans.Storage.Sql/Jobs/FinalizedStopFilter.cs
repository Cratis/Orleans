// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Orleans.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Orleans.Storage.Sql.Jobs;

/// <summary>
/// Chooses the SQL predicate that leaves stopped jobs with a finalized stop out of a read, per database provider.
/// </summary>
internal static class FinalizedStopFilter
{
    /// <summary>
    /// Gets the SQL predicate for a provider.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/> being queried.</param>
    /// <param name="statusColumn">The already delimited name of the status column.</param>
    /// <param name="stateColumn">The already delimited name of the serialized state column.</param>
    /// <returns>The predicate, or null when the provider has none and the read must filter after materializing.</returns>
    /// <remarks>
    /// A row with no <c lang="csharp">stopFinalized</c> element (written before finalization existed), or whose state is empty or not
    /// valid JSON, is kept, matching how <c lang="csharp">ToJobState</c> reads it. PostgreSQL relies on the jsonb column
    /// created by the migrations, which cannot hold empty or invalid JSON. SQL Server uses nvarchar(max) guarded by
    /// ISJSON, and SQLite uses text guarded by json_valid. Providers without a predicate read through the fallback.
    /// </remarks>
    internal static string? For(DatabaseType databaseType, string statusColumn, string stateColumn) => databaseType switch
    {
        DatabaseType.Sqlite =>
            $"NOT ({statusColumn} = {(int)JobStatus.Stopped} AND CASE WHEN json_valid({stateColumn}) THEN COALESCE(json_extract({stateColumn}, '$.stopFinalized'), 0) ELSE 0 END = 1)",
        DatabaseType.PostgreSql =>
            $"NOT ({statusColumn} = {(int)JobStatus.Stopped} AND COALESCE(({stateColumn} -> 'stopFinalized') = 'true'::jsonb, FALSE))",
        DatabaseType.SqlServer =>
            $"NOT ({statusColumn} = {(int)JobStatus.Stopped} AND CASE WHEN ISJSON({stateColumn}) = 1 THEN CASE WHEN JSON_VALUE({stateColumn}, '$.stopFinalized') = N'true' THEN 1 ELSE 0 END ELSE 0 END = 1)",
        _ => null
    };

    /// <summary>
    /// Gets the predicate that leaves finalized stops out for the provider behind a jobs context.
    /// </summary>
    /// <param name="dbContext">The <see cref="JobsDbContext"/> whose provider and mapped columns to use.</param>
    /// <param name="table">The delimited name of the jobs table.</param>
    /// <returns>The predicate, or null when the provider has none and the read must filter after materializing.</returns>
    internal static string? For(JobsDbContext dbContext, out string table)
    {
        var entity = dbContext.Model.FindEntityType(typeof(Job))!;
        var tableName = entity.GetTableName()!;
        var schema = entity.GetSchema();
        var store = StoreObjectIdentifier.Table(tableName, schema);
        var helper = dbContext.GetService<ISqlGenerationHelper>();
        table = helper.DelimitIdentifier(tableName, schema);

        var databaseType = GetDatabaseType(dbContext);
        var status = helper.DelimitIdentifier(entity.FindProperty(nameof(Job.Status))!.GetColumnName(store)!);
        var state = helper.DelimitIdentifier(entity.FindProperty(nameof(Job.StateJson))!.GetColumnName(store)!);
        return For(databaseType, status, state);
    }

    static DatabaseType GetDatabaseType(JobsDbContext dbContext)
    {
        try
        {
            return dbContext.Database.GetDatabaseType();
        }
        catch (UnsupportedDatabaseType)
        {
            // A provider this storage has no predicate for reads through the fallback.
            return DatabaseType.Unknown;
        }
    }
}
