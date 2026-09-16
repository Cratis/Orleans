// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Hosting;

/// <summary>
/// Represents the clustering mode for a co-hosted Orleans silo.
/// </summary>
public enum ClusteringMode
{
    /// <summary>
    /// Localhost clustering for single-instance development.
    /// </summary>
    Localhost = 0,

    /// <summary>
    /// Durable MongoDB cluster membership for running more than one instance.
    /// </summary>
    MongoDB = 1
}
