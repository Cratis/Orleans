// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Represents the rules by which the jobs storage names the MongoDB databases it stores its data in.
/// </summary>
/// <remarks>
/// This is the one place the naming is decided. Anything resolving a jobs database must go through here rather
/// than composing the name again, since a second copy of the rule is free to drift and a database name that does
/// not match is not an error but an empty result.
/// </remarks>
public static class DatabaseNames
{
    /// <summary>
    /// The maximum length, in bytes, MongoDB allows for a database name.
    /// </summary>
    const int MaximumLengthInBytes = 63;

    /// <summary>
    /// The characters MongoDB does not allow in a database name.
    /// </summary>
    static readonly SearchValues<char> _invalidCharacters = SearchValues.Create(['/', '\\', '.', ' ', '"', '$', '*', '<', '>', ':', '|', '?', '\0']);

    /// <summary>
    /// Get the name of the database holding the jobs and job steps of a namespace within a scope.
    /// </summary>
    /// <param name="scope">The scope to get the database name for.</param>
    /// <param name="namespace">The namespace within the scope to get the database name for.</param>
    /// <returns>The database name.</returns>
    /// <remarks>
    /// Unlike the namespaced databases, an empty namespace is not suffixed — its jobs live in the bare
    /// <c lang="csharp">{scope}+jobs</c> name. A reader that suffixes unconditionally therefore resolves a database that simply
    /// does not exist for the default namespace, and reads come back empty rather than failing.
    /// </remarks>
    public static string ForJobs(string scope, string @namespace) =>
        Validated(
            string.IsNullOrEmpty(@namespace) ? $"{scope}+jobs" : $"{scope}+jobs+{@namespace}",
            scope,
            @namespace);

    static string Validated(string databaseName, string scope, string @namespace)
    {
        var reason = GetInvalidReason(databaseName);
        if (reason is not null)
        {
            throw new InvalidDatabaseName(databaseName, reason, scope, @namespace);
        }

        return databaseName;
    }

    static string? GetInvalidReason(string databaseName)
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            return "a database name cannot be empty";
        }

        var bytes = Encoding.UTF8.GetByteCount(databaseName);
        if (bytes > MaximumLengthInBytes)
        {
            return $"its UTF-8 representation is {bytes} bytes long, and MongoDB allows at most {MaximumLengthInBytes}";
        }

        var invalidIndex = databaseName.AsSpan().IndexOfAny(_invalidCharacters);
        if (invalidIndex >= 0)
        {
            return $"it contains the character '{databaseName[invalidIndex]}', which MongoDB does not allow in a database name";
        }

        return null;
    }
}
