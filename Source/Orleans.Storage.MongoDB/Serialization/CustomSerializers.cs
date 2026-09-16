// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Orleans.Storage.MongoDB.Serialization;

/// <summary>
/// Represents an implementation of <see cref="ICustomSerializers"/> that registers custom BSON serializers.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="types">The <see cref="ITypes"/>.</param>
[Singleton]
public class CustomSerializers(IServiceProvider serviceProvider, ITypes types) : ICustomSerializers
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

    static bool IsEligibleForAutoRegistration(Type type) => type.Assembly.FullName!.Contains("Cratis.Orleans") &&
                                                            !type.IsGenericType &&
                                                            !type.HasAttribute<BsonSerializerDisableAutoRegistrationAttribute>();
}
