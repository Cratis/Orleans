// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage;

/// <summary>
/// Defines a system that resolves <see cref="JobsStorage"/> for a scope and namespace.
/// </summary>
/// <remarks>
/// A scope is the opaque first part of a job grain's key — for Chronicle it is the event store name, for an
/// application it might be the tenant or the service — and the namespace is the second part. Storage
/// implementations decide what the pair resolves to; the engine only ever hands it back verbatim.
/// </remarks>
public interface IJobsStorage
{
    /// <summary>
    /// Get the <see cref="JobsStorage"/> for a specific scope and namespace.
    /// </summary>
    /// <param name="scope">The scope to get for.</param>
    /// <param name="namespace">The namespace within the scope to get for.</param>
    /// <returns><see cref="JobsStorage"/> for the scope and namespace.</returns>
    JobsStorage GetFor(string scope, string @namespace);

    /// <summary>
    /// Forget everything resolved so far, so the next resolution builds it again.
    /// </summary>
    /// <remarks>
    /// Resolution is cached, and part of resolving can be one-time work against the underlying store - the SQL
    /// storage applies its migrations there. An application that empties or recreates that store behind the
    /// job system leaves the cache pointing at something that no longer exists, and the one-time work never
    /// runs again. Nothing fails at the moment of the reset; the next write is what reports a table that is not
    /// there. Call this after emptying the store.
    /// </remarks>
    void Reset();
}
