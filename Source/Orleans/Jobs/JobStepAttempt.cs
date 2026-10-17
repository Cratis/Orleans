// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Carries the attempt a step reports its outcome for, next to the execution generation, without changing the job's
/// grain interface.
/// </summary>
/// <remarks>
/// A report that carries no attempt - from a release that does not know them - is attempt 1.
/// </remarks>
internal static class JobStepAttempt
{
    /// <summary>
    /// The request context key the attempt travels under.
    /// </summary>
    public const string Key = "Cratis.Orleans.Jobs.ReportedAttempt";

    /// <summary>
    /// Reads and removes the attempt a report carries.
    /// </summary>
    /// <returns>The attempt, or 1 when the report carries none.</returns>
    public static int TakeReported()
    {
        var attempt = RequestContext.Get(Key) is int value ? value : 1;
        RequestContext.Remove(Key);
        return attempt;
    }

    /// <summary>
    /// Makes the calls that follow report the given attempt.
    /// </summary>
    /// <param name="attempt">The attempt the step is on.</param>
    /// <returns>A scope that removes the attempt again when disposed.</returns>
    public static Carried Report(int attempt)
    {
        RequestContext.Set(Key, attempt);
        return new(Key);
    }

    /// <summary>
    /// A scope that keeps the attempt on the request context until disposed.
    /// </summary>
    /// <param name="key">The key the attempt travels under.</param>
    internal readonly struct Carried(string key) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => RequestContext.Remove(key);
    }
}
