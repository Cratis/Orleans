// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Orleans.Jobs;
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
        var jsonSerializerOptions = serviceProvider.GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions();
        storage = new JobsStorage(
            new Jobs.JobStorage(ContextFactory, jobTypes, jsonSerializerOptions),
            new Jobs.JobStepStorage(ContextFactory));
        _storageByScopeAndNamespace.TryAdd(key, storage);
        return storage;
    }
}
