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
}
