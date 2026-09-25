// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.Serialization;

/// <summary>
/// Represents the options for <see cref="CustomSerializers"/>.
/// </summary>
public class CustomSerializersOptions
{
    /// <summary>
    /// Gets or sets the assembly name fragments whose BSON serializers and serialization providers are registered.
    /// </summary>
    /// <remarks>
    /// Auto-registration activates the types it finds, so it is deliberately not applied to every assembly in the
    /// process. An application that keeps its own serializers outside this package adds its assembly here; without
    /// that, those serializers are found and silently skipped, and the driver reads the documents its own way.
    /// </remarks>
    public IList<string> AssemblyNameFragments { get; set; } = ["Cratis.Orleans"];
}
