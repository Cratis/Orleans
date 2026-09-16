// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// The exception that is thrown when a composed database name is not a legal MongoDB database name.
/// </summary>
/// <remarks>
/// The jobs storage composes its database names from the scope and the namespace the caller handed it. When the
/// scope is, say, a tenant-resolved value, an otherwise innocent looking id such as one containing a space
/// produces a name MongoDB rejects. Without this check MongoDB fails much later with a bare
/// <c language="csharp">Invalid namespace specified</c> that names neither the scope nor the namespace that caused it.
/// </remarks>
/// <param name="databaseName">The composed database name.</param>
/// <param name="reason">Why the name is not legal.</param>
/// <param name="scope">The scope it was composed from.</param>
/// <param name="namespace">The namespace it was composed from, if any.</param>
public class InvalidDatabaseName(string databaseName, string reason, string scope, string? @namespace)
    : Exception($"The MongoDB database name '{databaseName}' was composed from scope '{scope}'{(@namespace is null ? string.Empty : $" and namespace '{@namespace}'")}, but {reason}. Both the scope and the namespace must be legal MongoDB database name tokens.");
