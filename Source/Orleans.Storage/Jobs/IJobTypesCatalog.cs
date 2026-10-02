// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Optional capability for an <see cref="IJobTypes"/> registry to enumerate its persisted type names.
/// </summary>
/// <remarks>
/// Storage uses this catalog to exclude unregistered types before server-side paging and counting.
/// Registries without this capability remain supported through per-document filtering on reads.
/// </remarks>
public interface IJobTypesCatalog
{
    /// <summary>
    /// Gets all registered job types using their persisted names.
    /// </summary>
    IReadOnlyCollection<JobType> All { get; }
}
