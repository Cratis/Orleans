// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Hosting;

/// <summary>
/// Represents the options for the co-hosted Orleans silo.
/// </summary>
public class CratisOrleansOptions
{
    /// <summary>
    /// Gets whether the silo is co-hosted at all.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the clustering mode — localhost for single-instance development, MongoDB for durable membership.
    /// </summary>
    public ClusteringMode Clustering { get; init; } = ClusteringMode.Localhost;

    /// <summary>
    /// Gets the cluster id every instance joins.
    /// </summary>
    public string ClusterId { get; init; } = "default";

    /// <summary>
    /// Gets the service id every instance joins.
    /// </summary>
    public string ServiceId { get; init; } = "default";

    /// <summary>
    /// Gets the name of the MongoDB database holding cluster membership and grain state.
    /// </summary>
    public string DatabaseName { get; init; } = "orleans";

    /// <summary>
    /// Gets the MongoDB connection string to use.
    /// </summary>
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";
}
