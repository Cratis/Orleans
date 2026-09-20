// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SomeHost;

/// <summary>
/// A type in a host's own namespace - claimed by the host's registration on Orleans' shared JSON
/// codec, never by the Cratis serializer.
/// </summary>
/// <param name="Name">Some content.</param>
public record SomeHostRecord(string Name);
