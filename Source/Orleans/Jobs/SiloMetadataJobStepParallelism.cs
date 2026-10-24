// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Picks the number of job steps that may run in parallel on this silo from the silo's own metadata.
/// </summary>
/// <remarks>
/// The metadata is the one published with Orleans' UseSiloMetadata. A missing, unparsable or non-positive value falls back
/// to <see cref="JobsOptions.GetEffectiveMaxParallelSteps"/>, so a silo without the key behaves as it does without this hook.
/// </remarks>
/// <param name="siloMetadata">The metadata of the local silo.</param>
/// <param name="jobsOptions">The <see cref="JobsOptions"/> to fall back to.</param>
/// <param name="logger">Logger for logging.</param>
public class SiloMetadataJobStepParallelism(
    IOptions<SiloMetadata> siloMetadata,
    IOptions<JobsOptions> jobsOptions,
    ILogger<SiloMetadataJobStepParallelism> logger)
{
    /// <summary>
    /// The silo metadata key that holds the maximum number of parallel job steps, unless another key is configured.
    /// </summary>
    public const string DefaultMetadataKey = "cratis.jobs.max-parallel-steps";

    /// <summary>
    /// Gets the maximum number of job steps that may run in parallel on this silo.
    /// </summary>
    /// <param name="metadataKey">The silo metadata key that holds the limit.</param>
    /// <returns>The limit from the metadata, or the one from the <see cref="JobsOptions"/> when the metadata has no usable value.</returns>
    public int GetMaxParallelSteps(string metadataKey = DefaultMetadataKey)
    {
        if (!siloMetadata.Value.Metadata.TryGetValue(metadataKey, out var value))
        {
            return jobsOptions.Value.GetEffectiveMaxParallelSteps();
        }

        if (int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var limit) && limit > 0)
        {
            return limit;
        }

        logger.InvalidMaxParallelStepsMetadata(metadataKey, value);
        return jobsOptions.Value.GetEffectiveMaxParallelSteps();
    }
}
