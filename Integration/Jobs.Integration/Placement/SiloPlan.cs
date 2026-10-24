// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Placement;

/// <summary>
/// What a silo of the placement cluster is for, published to the cluster as silo metadata.
/// </summary>
/// <param name="Role">Either <see cref="ControllerRole"/> or <see cref="WorkerRole"/>.</param>
/// <param name="Preference">Higher is preferred.</param>
/// <param name="MaxParallelSteps">Optional per-silo step limit.</param>
public sealed record SiloPlan(string Role, int Preference, int? MaxParallelSteps = null)
{
    public const string ControllerRole = "controller";
    public const string WorkerRole = "worker";
    public const string RoleKey = "role";
    public const string PreferenceKey = "preference";

    public Dictionary<string, string> ToMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            [RoleKey] = Role,
            [PreferenceKey] = Preference.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        if (MaxParallelSteps is { } max)
        {
            metadata[SiloMetadataJobStepParallelism.DefaultMetadataKey] = max.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return metadata;
    }
}
