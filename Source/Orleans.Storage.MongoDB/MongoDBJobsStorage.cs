// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Represents a <see cref="IJobsStorage"/> implementation for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDBJobsStorage"/> class.
/// </remarks>
/// <param name="client">The <see cref="IMongoClient"/> to resolve databases from.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about the job type associations.</param>
/// <param name="customSerializers"><see cref="ICustomSerializers"/> that registers the BSON serializers job state relies on.</param>
/// <param name="options"><see cref="MongoDBJobsStorageOptions"/> to use.</param>
public class MongoDBJobsStorage(
    IMongoClient client,
    IJobTypes jobTypes,
    ICustomSerializers customSerializers,
    IOptions<MongoDBJobsStorageOptions> options) : IJobsStorage
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

        // Conventions first: they decide the element names the class maps are built with, and the filters
        // this storage uses assume those names. Registering after a class map has been built is too late.
        ConventionPacks.EnsureRegistered();
        customSerializers.Register();
        var databaseName = options.Value.DatabaseNameResolver?.Invoke(scope, @namespace)
            ?? DatabaseNames.ForJobs(scope, @namespace);
        var database = client.GetDatabase(databaseName);
        storage = new JobsStorage(new Jobs.JobStorage(database, jobTypes), new Jobs.JobStepStorage(database));
        _storageByScopeAndNamespace.TryAdd(key, storage);
        return storage;
    }
}
