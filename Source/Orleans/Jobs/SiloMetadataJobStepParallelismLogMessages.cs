// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

internal static partial class SiloMetadataJobStepParallelismLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Silo metadata '{MetadataKey}' has the value '{Value}', which is not a positive whole number; using the configured job step parallelism instead")]
    internal static partial void InvalidMaxParallelStepsMetadata(this ILogger<SiloMetadataJobStepParallelism> logger, string metadataKey, string value);
}
