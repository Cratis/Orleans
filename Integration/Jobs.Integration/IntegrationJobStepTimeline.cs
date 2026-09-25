// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Records when each <see cref="IntegrationJobStep"/> performed its item, so a spec can observe the order steps ran in.
/// </summary>
public static class IntegrationJobStepTimeline
{
    static readonly ConcurrentDictionary<string, IntegrationJobStepPerformed> _performed = new();

    /// <summary>
    /// Record that a step performed an item.
    /// </summary>
    /// <param name="performed">The <see cref="IntegrationJobStepPerformed"/>.</param>
    public static void Record(IntegrationJobStepPerformed performed) => _performed[performed.Item] = performed;

    /// <summary>
    /// Get when an item was performed.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The <see cref="IntegrationJobStepPerformed"/>, or null if no step performed it.</returns>
    public static IntegrationJobStepPerformed? For(string item) => _performed.GetValueOrDefault(item);
}
