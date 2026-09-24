// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Orleans.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Storage.Sql;

/// <summary>
/// Represents a <see cref="IJobsStorage"/> implementation for SQL databases through EF Core.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SqlJobsStorage"/> class.
/// </remarks>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about the job type associations.</param>
/// <param name="options"><see cref="SqlJobsStorageOptions"/> to use.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving the host's <see cref="JsonSerializerOptions"/>.</param>
public class SqlJobsStorage(
    IJobTypes jobTypes,
    IOptions<SqlJobsStorageOptions> options,
    IServiceProvider serviceProvider) : IJobsStorage
{
    static readonly Lock _migrationLock = new();
    readonly ConcurrentDictionary<string, JobsStorage> _storageByScopeAndNamespace = new();

    /// <inheritdoc/>
    public JobsStorage GetFor(string scope, string @namespace)
    {
        var key = KeyHelper.Combine(scope, @namespace);
        if (_storageByScopeAndNamespace.TryGetValue(key, out var storage))
        {
            return storage;
        }

        var contextOptions = options.Value.OptionsResolver(scope, @namespace);
        Jobs.JobsDbContext ContextFactory() => new(contextOptions);
        EnsureSchema(ContextFactory);
        var jsonSerializerOptions = serviceProvider.GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions();
        storage = new JobsStorage(
            new Jobs.JobStorage(ContextFactory, jobTypes, jsonSerializerOptions),
            new Jobs.JobStepStorage(ContextFactory));
        _storageByScopeAndNamespace.TryAdd(key, storage);
        return storage;
    }

    /// <summary>
    /// Applies the jobs migrations so the tables exist before anything reads or writes them.
    /// </summary>
    /// <param name="contextFactory">Factory creating the <see cref="Jobs.JobsDbContext"/> to migrate.</param>
    /// <remarks>
    /// A scope and namespace can map to a database that nothing has provisioned yet, and the host only ever
    /// hands us options - it never sees the context - so this is the one place that can guarantee the schema
    /// is there. It runs once per scope and namespace, because <c lang="csharp">GetFor</c> caches the storage
    /// it builds. Concurrent callers are serialized so two of them cannot race to create the same tables.
    /// </remarks>
    static void EnsureSchema(Func<Jobs.JobsDbContext> contextFactory)
    {
        lock (_migrationLock)
        {
            using var dbContext = contextFactory();
            dbContext.Database.Migrate();
        }
    }
}
