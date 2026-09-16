// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the key for a job.
/// </summary>
/// <param name="Scope">The storage scope the job belongs to — opaque to the engine; Chronicle uses the event store name, an application might use the tenant.</param>
/// <param name="Namespace">The namespace within the scope the job belongs to.</param>
public record JobKey(string Scope, string Namespace)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly JobKey NotSet = new(string.Empty, string.Empty);

    /// <summary>
    /// Implicitly convert from string to <see cref="JobKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(Scope, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobKey"/> instance.</returns>
    public static JobKey Parse(string key) => KeyHelper.Parse<JobKey>(key);
}
