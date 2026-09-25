// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Orleans.Storage.MongoDB.Serialization;

/// <summary>
/// Represents an implementation of <see cref="ICustomSerializers"/> that registers custom BSON serializers.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="types">The <see cref="ITypes"/>.</param>
/// <param name="options">The <see cref="CustomSerializersOptions"/> naming the assemblies to register from.</param>
[Singleton]
public class CustomSerializers(IServiceProvider serviceProvider, ITypes types, IOptions<CustomSerializersOptions> options) : ICustomSerializers
{
    static bool _isRegistered;

    /// <inheritdoc/>
    public void Register()
    {
        if (_isRegistered)
        {
            return;
        }

        // The MongoDB driver 3.x defaults Guid serialization to Unspecified, which throws the moment a Guid
        // is rendered - into a filter, a key or a document. Everything Cratis stores uses the standard
        // representation.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // Concepts (ConceptAs<T>) serialize as their underlying value through Arc's serialization provider.
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());

        foreach (var type in types.FindMultiple<IBsonSerializationProvider>().Where(IsEligibleForAutoRegistration))
        {
            var provider = (IBsonSerializationProvider)ActivatorUtilities.CreateInstance(serviceProvider, type);
            BsonSerializer.RegisterSerializationProvider(provider);
        }
        foreach (var type in types.FindMultiple<IBsonSerializer>().Where(IsEligibleForAutoRegistration))
        {
            var serializer = (IBsonSerializer)ActivatorUtilities.CreateInstance(serviceProvider, type);
            if (BsonSerializer.LookupSerializer(serializer.ValueType) is not null)
            {
                continue;
            }
            BsonSerializer.TryRegisterSerializer(serializer.ValueType, serializer);
        }
        _isRegistered = true;
    }

    /// <summary>
    /// Decides whether a discovered type is registered.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> that was discovered.</param>
    /// <param name="assemblyNameFragments">The assembly name fragments that are registered from.</param>
    /// <returns>True if the type is registered, false if it is skipped.</returns>
    internal static bool IsEligibleForAutoRegistration(Type type, IEnumerable<string> assemblyNameFragments) =>
        assemblyNameFragments.Any(fragment => type.Assembly.FullName!.Contains(fragment, StringComparison.Ordinal)) &&
        !type.IsGenericType &&
        !type.HasAttribute<BsonSerializerDisableAutoRegistrationAttribute>();

    bool IsEligibleForAutoRegistration(Type type) => IsEligibleForAutoRegistration(type, options.Value.AssemblyNameFragments);
}
