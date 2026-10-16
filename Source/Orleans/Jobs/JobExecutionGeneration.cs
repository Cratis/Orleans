// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Carries a job's execution generation between the job and its steps in the Orleans request context.
/// </summary>
/// <remarks>
/// <para>
/// A job begins an execution when it starts and each time it resumes from a stop. The generation is the position of
/// that status change in the job's status history, so it is derived from what every release persists and never stored
/// on its own. A step stores the generation it was started in and reports its outcome with it, which lets the job tell
/// a report from an earlier execution apart from one of the current execution.
/// </para>
/// <para>
/// The request context is used rather than new grain methods because a silo that does not know the key ignores it, so
/// a cluster running mixed releases keeps working. Each direction has a key of its own: a step from a release without
/// generations never removes the key its job sends, and its work and reports inherit it, so the job reads only the
/// key a step sets deliberately when it reports. A value is removed as soon as it has been read, so it never flows
/// on into calls the receiver makes - including the work a step performs.
/// </para>
/// </remarks>
internal static class JobExecutionGeneration
{
    /// <summary>
    /// The request context key a job sends its generation to its steps under.
    /// </summary>
    public const string Key = "Cratis.Orleans.Jobs.Generation";

    /// <summary>
    /// The request context key a step reports the generation of its run to its job under.
    /// </summary>
    public const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    /// <summary>
    /// Takes the generation a job sent with the current call and removes every generation from the request context.
    /// </summary>
    /// <returns>The generation, or null when the caller did not send one.</returns>
    public static long? Take() => TakeFrom(Key);

    /// <summary>
    /// Takes the generation a step reported with the current call and removes every generation from the request context.
    /// </summary>
    /// <returns>The generation, or null when the step did not report one - as a step from an older release never does.</returns>
    public static long? TakeReported() => TakeFrom(ReportedKey);

    /// <summary>
    /// Sends a job's generation with the calls made from the current flow until the returned scope is disposed.
    /// </summary>
    /// <param name="generation">The generation to send, or null to send none.</param>
    /// <returns>The scope that removes the generation again.</returns>
    public static Carried Carry(long? generation) => CarryUnder(Key, generation);

    /// <summary>
    /// Reports a step run's generation with the calls made from the current flow until the returned scope is disposed.
    /// </summary>
    /// <param name="generation">The generation to report, or null to report none.</param>
    /// <returns>The scope that removes the generation again.</returns>
    public static Carried Report(long? generation) => CarryUnder(ReportedKey, generation);

    static long? TakeFrom(string key)
    {
        var generation = RequestContext.Get(key) is long value ? value : (long?)null;
        RequestContext.Remove(Key);
        RequestContext.Remove(ReportedKey);
        return generation;
    }

    static Carried CarryUnder(string key, long? generation)
    {
        // Only the key of this direction travels; a generation inherited for the other direction never does.
        RequestContext.Remove(Key);
        RequestContext.Remove(ReportedKey);
        if (generation is { } value)
        {
            RequestContext.Set(key, value);
        }
        return new(key);
    }

    /// <summary>
    /// Represents a generation sent with calls until it is disposed.
    /// </summary>
    /// <param name="key">The request context key the generation is sent under.</param>
    internal readonly struct Carried(string key) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => RequestContext.Remove(key);
    }
}
