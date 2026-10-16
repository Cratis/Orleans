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
/// a cluster running mixed releases keeps working. A value is removed as soon as it has been read, so it never flows
/// on into calls the receiver makes - including the work a step performs.
/// </para>
/// </remarks>
internal static class JobExecutionGeneration
{
    /// <summary>
    /// The request context key the generation travels under.
    /// </summary>
    public const string Key = "Cratis.Orleans.Jobs.Generation";

    /// <summary>
    /// Takes the generation the current call carries and removes it from the request context.
    /// </summary>
    /// <returns>The generation, or null when the caller did not send one.</returns>
    public static long? Take()
    {
        var generation = RequestContext.Get(Key) is long value ? value : (long?)null;
        RequestContext.Remove(Key);
        return generation;
    }

    /// <summary>
    /// Sends a generation with the calls made from the current flow until the returned scope is disposed.
    /// </summary>
    /// <param name="generation">The generation to send, or null to send none.</param>
    /// <returns>The scope that removes the generation again.</returns>
    public static Carried Carry(long? generation)
    {
        if (generation is { } value)
        {
            RequestContext.Set(Key, value);
        }
        else
        {
            RequestContext.Remove(Key);
        }
        return default;
    }

    /// <summary>
    /// Represents a generation sent with calls until it is disposed.
    /// </summary>
    internal readonly struct Carried : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => RequestContext.Remove(Key);
    }
}
