// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Orleans.Jobs;

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
    /// valid JSON, is kept, matching how <c lang="csharp">ToJobState</c> reads it. Only SQLite has a predicate: PostgreSQL and
    /// SQL Server keep the read-then-filter fallback until the same predicate is verified against a real server.
    /// </remarks>
    internal static string? For(DatabaseType databaseType, string statusColumn, string stateColumn) => databaseType switch
    {
        DatabaseType.Sqlite =>
            $"NOT ({statusColumn} = {(int)JobStatus.Stopped} AND CASE WHEN json_valid({stateColumn}) THEN COALESCE(json_extract({stateColumn}, '$.stopFinalized'), 0) ELSE 0 END = 1)",
        _ => null
    };
}
