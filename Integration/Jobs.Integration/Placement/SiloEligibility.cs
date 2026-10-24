// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Orleans.Jobs.Integration.Placement;

public static class SiloEligibility
{
    /// <summary>
    /// Gets the candidates carrying the role, the most preferred first.
    /// </summary>
    /// <param name="candidates">The compatible silos.</param>
    /// <param name="cache">The silo metadata.</param>
    /// <param name="role">The role to look for.</param>
    /// <returns>The eligible silos, best first.</returns>
    public static IReadOnlyList<SiloAddress> WithRole(IEnumerable<SiloAddress> candidates, ISiloMetadataCache cache, string role) =>
        candidates
            .Select(silo => (Silo: silo, cache.GetSiloMetadata(silo).Metadata))
            .Where(candidate => candidate.Metadata.TryGetValue(SiloPlan.RoleKey, out var found) && found == role)
            .OrderByDescending(candidate => candidate.Metadata.TryGetValue(SiloPlan.PreferenceKey, out var preference)
                && int.TryParse(preference, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
            .ThenBy(candidate => candidate.Silo.ToString(), StringComparer.Ordinal)
            .Select(candidate => candidate.Silo)
            .ToList();
}
