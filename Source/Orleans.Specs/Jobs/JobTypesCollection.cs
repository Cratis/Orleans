// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Serializes specs that construct the process-wide registry or use serializers that read it.
/// </summary>
[CollectionDefinition("JobTypes", DisableParallelization = true)]
public class JobTypesCollection;
