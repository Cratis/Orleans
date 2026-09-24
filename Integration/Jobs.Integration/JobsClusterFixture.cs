// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Cratis.Orleans.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// A real co-hosted Orleans silo running the job system end to end against a real MongoDB: started through
/// the public <c lang="csharp">AddCratisOrleans</c> surface exactly as an application would. Requires a MongoDB on
/// <c lang="csharp">mongodb://localhost:27017</c>.
/// </summary>
public class JobsClusterFixture : IDisposable
{
    /// <summary>
    /// The MongoDB connection string the cluster and its storage use.
    /// </summary>
    public const string ConnectionString = "mongodb://localhost:27017";

    /// <summary>
    /// The storage scope the specs run under.
    /// </summary>
    public const string Scope = "integration-jobs";

    /// <summary>
    /// The database the jobs land in.
    /// </summary>
    public const string DatabaseName = "orleans-integration-jobs";

    static readonly Lazy<JobsClusterFixture> _shared = new(() => new JobsClusterFixture(), isThreadSafe: true);

    readonly WebApplication _app;

    /// <summary>
    /// Gets the one silo every spec shares. Starting a silo per spec class is slow and, because they all
    /// target the same database, they would wipe each other's state on reset.
    /// </summary>
    public static JobsClusterFixture Shared => _shared.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsClusterFixture"/> class.
    /// </summary>
    public JobsClusterFixture()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddConsole();

        // Bind an ephemeral port. The silo is only ever reached in-process through its grain factory, and a
        // fixed port means a second spec class - or anything else on the machine - cannot start one.
        builder.Configuration["urls"] = "http://127.0.0.1:0";

        builder.AddCratisOrleans(new CratisOrleansOptions
        {
            ClusterId = Scope,
            ServiceId = Scope
        });
        builder.Services.AddSingleton<ITypes>(_ => new Cratis.Types.Types());

        // The step grains resolve this from the silo, so it has to be registered before the silo starts. One
        // instance for the whole silo; each spec resets it.
        builder.Services.AddSingleton<for_JobsManager.given.TheJobStepProcessor>();
        builder.Services.AddCratisOrleansMongoDBJobsStorage(
            new MongoClient(ConnectionString),
            options => options.DatabaseNameResolver = (_, _) => DatabaseName);

        _app = builder.Build();
        _app.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the grain factory of the running silo.
    /// </summary>
    public IGrainFactory GrainFactory => _app.Services.GetRequiredService<IGrainFactory>();

    /// <summary>
    /// Gets the services of the running silo, for reaching the storage the job system actually wrote to.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Wipes the jobs database, so every spec starts from nothing.
    /// </summary>
    public static void ResetStorage() =>
        new MongoClient(ConnectionString).DropDatabase(DatabaseName);

    /// <inheritdoc/>
    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
