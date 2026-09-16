// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the key for a job step.
/// </summary>
/// <param name="JobId">The job the step is for.</param>
/// <param name="Scope">The storage scope the job step belongs to — opaque to the engine.</param>
/// <param name="Namespace">The namespace within the scope the job step belongs to.</param>
public record JobStepKey(JobId JobId, string Scope, string Namespace)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="JobStepKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobStepKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobStepKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobStepKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(JobId, Scope, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobStepKey"/> instance.</returns>
    public static JobStepKey Parse(string key) => KeyHelper.Parse<JobStepKey>(key);
}
