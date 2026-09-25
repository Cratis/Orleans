// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Cratis.Orleans.Setup;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// A real co-hosted Orleans silo running the job system end to end against SQL storage, started through the
/// public <c lang="csharp">AddCratisOrleans</c> surface exactly as an application would.
/// </summary>
/// <remarks>
/// The MongoDB fixture covers the job system's behavior; this one exists because the SQL storage is reached
/// through the same grain-storage providers and nothing was running that path. Storage-level specs write
/// through the storage directly and so never see what the provider hands it.
/// </remarks>
public class SqlJobsClusterFixture : IDisposable
{
    /// <summary>
    /// The storage scope the specs run under.
    /// </summary>
    public const string Scope = "integration-sql-jobs";

    static readonly Lazy<SqlJobsClusterFixture> _shared = new(() => new SqlJobsClusterFixture(), isThreadSafe: true);

    readonly WebApplication _app;
    readonly string _databasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlJobsClusterFixture"/> class.
    /// </summary>
    public SqlJobsClusterFixture()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cratis-orleans-sql-jobs-{Guid.NewGuid():N}.db");

        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddConsole();
        builder.Configuration["urls"] = "http://127.0.0.1:0";

        builder.AddCratisOrleans(new CratisOrleansOptions
        {
            ClusterId = Scope,
            ServiceId = Scope
        });
        builder.Services.AddSingleton<ITypes>(_ => new Cratis.Types.Types());
        builder.Services.AddSingleton<for_JobsManager.given.TheJobStepProcessor>();

        var path = _databasePath;
        var postgres = Environment.GetEnvironmentVariable("CRATIS_ORLEANS_SQL_POSTGRES");
        builder.Services.AddCratisOrleansSqlJobsStorage(options =>
            options.OptionsResolver = (_, _) => string.IsNullOrEmpty(postgres)
                ? new DbContextOptionsBuilder<JobsDbContext>().UseSqlite($"Data Source={path}").Options
                : new DbContextOptionsBuilder<JobsDbContext>().UseNpgsql(postgres).Options);

        _app = builder.Build();
        _app.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the one silo every SQL spec shares.
    /// </summary>
    public static SqlJobsClusterFixture Shared => _shared.Value;

    /// <summary>
    /// Gets the grain factory of the running silo.
    /// </summary>
    public IGrainFactory GrainFactory => _app.Services.GetRequiredService<IGrainFactory>();

    /// <summary>
    /// Gets the services of the running silo, for reaching the storage the job system actually wrote to.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <inheritdoc/>
    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        GC.SuppressFinalize(this);
    }
}
