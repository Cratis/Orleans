// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Resolves singleton catalogs independently per pass, isolating constructor failures from other catalogs.
/// </summary>
/// <param name="providers">Catalog types and their singleton resolvers.</param>
/// <param name="logger">The recovery logger.</param>
internal sealed class JobsManagerKeyProviders(
    IEnumerable<(Type Type, Func<IJobsManagerKeys> Resolve)> providers,
    ILogger<JobsRecovery> logger) : IInstancesOf<IJobsManagerKeys>
{
    /// <inheritdoc/>
    public IEnumerator<IJobsManagerKeys> GetEnumerator()
    {
        foreach (var (type, resolve) in providers)
        {
            IJobsManagerKeys provider;
            try
            {
                provider = resolve();
            }
            catch (Exception exception)
            {
                logger.KeyProviderFailed(exception, type);
                continue;
            }
            yield return provider;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
