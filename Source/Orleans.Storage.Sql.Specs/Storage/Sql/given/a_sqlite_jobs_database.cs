// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.Data.Sqlite;
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
/// SQLite database disappears the moment its last connection closes. Each spec gets its own temporary
/// directory, so the database and any side files SQLite writes next to it (<c lang="text">-wal</c>, <c lang="text">-shm</c>,
/// <c lang="text">-journal</c>) are removed together when the spec is disposed.
/// </remarks>
public class a_sqlite_jobs_database : Specification, IDisposable
{
    string _databaseDirectory;
    protected string _databasePath;
    protected DbContextOptions<JobsDbContext> _contextOptions;

    void Establish()
    {
        _databaseDirectory = Path.Combine(Path.GetTempPath(), $"cratis-orleans-jobs-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_databaseDirectory);
        _databasePath = Path.Combine(_databaseDirectory, "jobs.db");
        _contextOptions = new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
    }

    protected JobsDbContext CreateContext() => new(_contextOptions);

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // The storage opens a fresh context per operation, so pooled connections can still hold the
        // file open. Release them first, otherwise the files cannot be deleted on every platform.
        SqliteConnection.ClearAllPools();

        if (_databaseDirectory is not null && Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, recursive: true);
        }
    }
}
