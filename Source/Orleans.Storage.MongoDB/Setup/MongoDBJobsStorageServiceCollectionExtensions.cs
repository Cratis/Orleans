// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Orleans.Setup;

/// <summary>
/// Extension methods for setting up MongoDB jobs storage.
/// </summary>
public static class MongoDBJobsStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add MongoDB jobs storage for the Orleans job system.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="client">The <see cref="IMongoClient"/> to use.</param>
    /// <param name="configure">Optional callback for configuring <see cref="MongoDBJobsStorageOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisOrleansMongoDBJobsStorage(
        this IServiceCollection services,
        IMongoClient client,
        Action<MongoDBJobsStorageOptions>? configure = default)
    {
        services.AddSingleton(client);
        if (configure is not null)
        {
            services.Configure(configure);
        }
        services.AddSingleton<ICustomSerializers, CustomSerializers>();
        services.AddSingleton<IJobsStorage, MongoDBJobsStorage>();
        return services;
    }
}
