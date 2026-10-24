// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.for_FinalizedStopFilter;

public class when_choosing_for_a_context : Specification
{
    JobsDbContext _sqlite;
    JobsDbContext _postgres;
    JobsDbContext _sqlServer;
    string _sqlitePredicate;
    string _postgresPredicate;
    string _sqlServerPredicate;
    string _sqliteTable;
    string _postgresTable;
    string _sqlServerTable;

    void Establish()
    {
        _sqlite = new(new DbContextOptionsBuilder<JobsDbContext>().UseSqlite("Data Source=:memory:").Options);
        _postgres = new(new DbContextOptionsBuilder<JobsDbContext>().UseNpgsql("Host=localhost;Database=specs").Options);
        _sqlServer = new(new DbContextOptionsBuilder<JobsDbContext>().UseSqlServer("Server=localhost;Database=specs").Options);
    }

    void Because()
    {
        _sqlitePredicate = FinalizedStopFilter.For(_sqlite, out _sqliteTable);
        _postgresPredicate = FinalizedStopFilter.For(_postgres, out _postgresTable);
        _sqlServerPredicate = FinalizedStopFilter.For(_sqlServer, out _sqlServerTable);
    }

    [Fact] void should_delimit_the_sqlite_table() => _sqliteTable.ShouldEqual("\"Jobs\"");
    [Fact] void should_delimit_the_postgresql_table() => _postgresTable.ShouldEqual("\"Jobs\"");
    [Fact] void should_delimit_the_sql_server_table() => _sqlServerTable.ShouldEqual("[Jobs]");
    [Fact] void should_choose_sqlite() => _sqlitePredicate.ShouldEqual(FinalizedStopFilter.For(DatabaseType.Sqlite, "\"Status\"", "\"StateJson\""));
    [Fact] void should_choose_postgresql() => _postgresPredicate.ShouldEqual(FinalizedStopFilter.For(DatabaseType.PostgreSql, "\"Status\"", "\"StateJson\""));
    [Fact] void should_choose_sql_server() => _sqlServerPredicate.ShouldEqual(FinalizedStopFilter.For(DatabaseType.SqlServer, "[Status]", "[StateJson]"));

    void Destroy()
    {
        _sqlite.Dispose();
        _postgres.Dispose();
        _sqlServer.Dispose();
    }
}
