// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Orleans.Storage.Sql.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_FinalizedStopFilter;

public class when_choosing_for_a_provider : Specification
{
    string _sqlite;
    string _postgres;
    string _sqlServer;
    string _unknown;

    void Because()
    {
        _sqlite = FinalizedStopFilter.For(DatabaseType.Sqlite, "\"Status\"", "\"StateJson\"");
        _postgres = FinalizedStopFilter.For(DatabaseType.PostgreSql, "\"Status\"", "\"StateJson\"");
        _sqlServer = FinalizedStopFilter.For(DatabaseType.SqlServer, "[Status]", "[StateJson]");
        _unknown = FinalizedStopFilter.For(DatabaseType.Unknown, "Status", "StateJson");
    }

    [Fact] void should_filter_in_storage_for_sqlite() => _sqlite.ShouldNotBeNull();
    [Fact] void should_name_the_stopped_status_from_the_enum() => _sqlite.ShouldContain("\"Status\" = 7");
    [Fact] void should_filter_in_storage_for_postgresql() => _postgres.ShouldEqual("NOT (\"Status\" = 7 AND COALESCE((\"StateJson\" -> 'stopFinalized') = 'true'::jsonb, FALSE))");
    [Fact] void should_filter_in_storage_for_sql_server() => _sqlServer.ShouldEqual("NOT ([Status] = 7 AND CASE WHEN ISJSON([StateJson]) = 1 THEN CASE WHEN JSON_VALUE([StateJson], '$.stopFinalized') = N'true' THEN 1 ELSE 0 END ELSE 0 END = 1)");
    [Fact] void should_not_contain_format_placeholders() => new[] { _sqlite, _postgres, _sqlServer }.All(predicate => !predicate.Contains('{') && !predicate.Contains('}')).ShouldBeTrue();
    [Fact] void should_fall_back_for_an_unknown_provider() => _unknown.ShouldBeNull();
}
