// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Hosting.for_CratisOrleansSiloBuilderExtensions;

public interface ITraceContextProbe : IGrainWithGuidKey
{
    Task<string[]> Read(bool relay);
}
