// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql.given;

/// <summary>
/// A real SQLite database on disk, backing a <see cref="JobsDbContext"/>.
/// </summary>
/// <remarks>
/// These specs deliberately run against a real EF Core provider rather than substitutes. The defect they
/// exist to prevent - a provider that ships a context it cannot create tables for - is invisible to a
/// mocked <c lang="csharp">DbContext</c>, because the mock never touches a schema. A file rather than
/// <c lang="csharp">:memory:</c> because the storage opens a fresh context per operation, and an in-memory
/// SQLite database disappears the moment its last connection closes.
/// </remarks>
public class a_sqlite_jobs_database : Specification, IDisposable
{
    protected string _databasePath;
    protected DbContextOptions<JobsDbContext> _contextOptions;

    void Establish()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cratis-orleans-jobs-specs-{Guid.NewGuid():N}.db");
        _contextOptions = new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
    }

    protected JobsDbContext CreateContext() => new(_contextOptions);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
