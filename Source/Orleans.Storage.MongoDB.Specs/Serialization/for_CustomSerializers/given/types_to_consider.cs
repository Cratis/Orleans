// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.Serialization.for_CustomSerializers.given;

/// <summary>
/// The types the eligibility decision is made about.
/// </summary>
/// <remarks>
/// This assembly is named Cratis.Orleans.Storage.MongoDB.Specs, so a type declared here stands in for one the
/// package ships. A type from the MongoDB driver stands in for one belonging to an application that has not been
/// named in the options.
/// </remarks>
public static class types_to_consider
{
    /// <summary>
    /// Gets a type from an assembly matching the default fragment.
    /// </summary>
    public static Type FromThePackage => typeof(a_serializer);

    /// <summary>
    /// Gets a type from an assembly that no default fragment matches.
    /// </summary>
    public static Type FromAnotherAssembly => typeof(global::MongoDB.Bson.BsonDocument);

    /// <summary>
    /// Gets a generic type from an assembly matching the default fragment.
    /// </summary>
    public static Type Generic => typeof(a_generic_serializer<>);

    /// <summary>
    /// Gets a type that opted out of auto registration.
    /// </summary>
    public static Type OptedOut => typeof(a_serializer_that_opted_out);

    /// <summary>
    /// A type standing in for a serializer the package ships.
    /// </summary>
    public class a_serializer;

    /// <summary>
    /// A generic type standing in for a serializer that cannot be activated without its type argument.
    /// </summary>
    /// <typeparam name="T">The type argument.</typeparam>
    public class a_generic_serializer<T>;

    /// <summary>
    /// A type standing in for a serializer that is registered by hand rather than discovered.
    /// </summary>
    [BsonSerializerDisableAutoRegistration]
    public class a_serializer_that_opted_out;
}
