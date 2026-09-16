// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Setup;

/// <summary>
/// Extension methods for setting up SQL jobs storage.
/// </summary>
public static class SqlJobsStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add SQL jobs storage for the Orleans job system.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Callback for configuring <see cref="SqlJobsStorageOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisOrleansSqlJobsStorage(
        this IServiceCollection services,
        Action<SqlJobsStorageOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IJobsStorage, SqlJobsStorage>();
        return services;
    }
}
