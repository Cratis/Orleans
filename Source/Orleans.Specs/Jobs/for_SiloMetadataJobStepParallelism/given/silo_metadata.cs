// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Orleans.Jobs.for_SiloMetadataJobStepParallelism.given;

public class silo_metadata : Specification
{
    public const string Key = SiloMetadataJobStepParallelism.DefaultMetadataKey;

    protected JobsOptions _jobsOptions;

    void Establish() => _jobsOptions = new() { MaxParallelSteps = 7 };

    public static SiloMetadata MetadataWith(params (string Key, string Value)[] pairs)
    {
        var metadata = new SiloMetadata();

        // The runtime fills the metadata through an internal method; the property has a private setter.
        typeof(SiloMetadata).GetProperty(nameof(SiloMetadata.Metadata))!.SetValue(metadata, pairs.ToImmutableDictionary(pair => pair.Key, pair => pair.Value));
        return metadata;
    }

    protected SiloMetadataJobStepParallelism ParallelismOf(SiloMetadata metadata) =>
        new(Options.Create(metadata), Options.Create(_jobsOptions), NullLogger<SiloMetadataJobStepParallelism>.Instance);
}
