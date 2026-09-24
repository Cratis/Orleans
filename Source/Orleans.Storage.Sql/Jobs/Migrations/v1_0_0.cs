// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Orleans.Storage.Sql.Jobs.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Creates the schema the SQL jobs storage reads and writes.
/// </summary>
/// <remarks>
/// This lives with the provider that owns the model on purpose. The provider previously shipped a
/// <see cref="JobsDbContext"/> with no way to create its own tables, which left every consumer to
/// provision the schema themselves - and the first one that forgot got a database where every job
/// read and write failed on a missing relation.
/// The column helpers come from Cratis.Arc.EntityFrameworkCore so a single migration is valid across
/// SQL Server, PostgreSQL and SQLite, which is the provider matrix this package supports.
/// </remarks>
[DbContext(typeof(JobsDbContext))]
[Migration($"Cratis-Orleans-Jobs-{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Jobs,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                Type = table.StringColumn(migrationBuilder, maxLength: 256),
                Status = table.NumberColumn<int>(migrationBuilder),
                Created = table.Column<DateTimeOffset>(nullable: false),
                StateJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Jobs}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Jobs}_Status",
            table: WellKnownTableNames.Jobs,
            column: "Status");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Jobs}_Type",
            table: WellKnownTableNames.Jobs,
            column: "Type");

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.JobSteps,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                JobId = table.GuidColumn(migrationBuilder, nullable: false),
                JobStepId = table.GuidColumn(migrationBuilder, nullable: false),
                Type = table.StringColumn(migrationBuilder, maxLength: 256),
                Name = table.StringColumn(migrationBuilder, maxLength: 256),
                Status = table.NumberColumn<int>(migrationBuilder),
                IsPrepared = table.BoolColumn(migrationBuilder),
                StateJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.JobSteps}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_JobId",
            table: WellKnownTableNames.JobSteps,
            column: "JobId");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_Status",
            table: WellKnownTableNames.JobSteps,
            column: "Status");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_Type",
            table: WellKnownTableNames.JobSteps,
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.JobSteps);
        migrationBuilder.DropTable(name: WellKnownTableNames.Jobs);
    }
}
