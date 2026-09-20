// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;

namespace Cratis.Orleans;

/// <summary>
/// Extension methods for configuring the Orleans serialization the Cratis stack relies on.
/// </summary>
public static class SerializationConfigurationExtensions
{
    /// <summary>
    /// Adds the serializers the Cratis types crossing grain boundaries need: concepts, OneOf, expando objects
    /// and a JSON fallback for everything else Cratis-owned that has no generated codec.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The JSON fallback is <see cref="CratisJsonSerializer"/>, a codec of its own rather than a claim on
    /// Orleans' shared <c language="csharp">JsonCodec</c>. The shared codec holds a single
    /// <c language="csharp">JsonCodecOptions.SerializerOptions</c> that every <c language="csharp">AddJsonSerializer</c>
    /// call overwrites, so a host registering a JSON serializer for its own namespaces - after these
    /// registrations, as any host naturally does - would silently replace the options the Cratis types
    /// depend on. Owning the codec keeps host serializer configuration unable to reach them.
    /// </remarks>
    public static IServiceCollection AddCratisOrleansSerializers(this IServiceCollection services)
    {
        services.AddSingleton(CratisJsonSerializer.CreateSerializerOptions());
        services.AddCustomSerializers();
        services.AddConceptSerializer();
        services.AddSingleton<ITypeFilter, CratisTypesFilter>();
        services.AddCompleteSerializer<CratisJsonSerializer>();
        services.AddSerializer();
        return services;
    }

    /// <summary>
    /// Adds a copier for LINQ internal collection types.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddLinqCollectionCopier(this IServiceCollection services)
    {
        services.AddSingleton<LinqCollectionCopier>();
        services.AddSingleton<IGeneralizedCopier, LinqCollectionCopier>();
        services.AddSingleton<ITypeFilter, LinqCollectionCopier>();

        return services;
    }

    /// <summary>
    /// Add a complete serializer, convenience method when a serializer implements all the interfaces.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <typeparam name="TSerializer">Type of serializer.</typeparam>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Each registration is its own singleton descriptor, so <typeparamref name="TSerializer"/> is instantiated
    /// once per interface it is registered for rather than once in total. State a serializer keeps in instance
    /// fields is therefore only shared between members of the same interface - anything written from
    /// <see cref="IGeneralizedCopier"/> and read from <see cref="IGeneralizedCodec"/> lands in different
    /// objects. Put state that must be shared in its own singleton and inject it.
    /// </remarks>
    public static IServiceCollection AddCompleteSerializer<TSerializer>(this IServiceCollection services)
        where TSerializer : class, IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
    {
        services.AddSingleton<TSerializer>();
        services.AddSingleton<IGeneralizedCodec, TSerializer>();
        services.AddSingleton<IGeneralizedCopier, TSerializer>();
        services.AddSingleton<ITypeFilter, TSerializer>();

        return services;
    }
    static IServiceCollection AddCustomSerializers(this IServiceCollection services)
    {
        services.AddCompleteSerializer<OneOfSerializer>();
        services.AddCompleteSerializer<ExpandoObjectSerializer>();
        services.AddLinqCollectionCopier();
        return services;
    }
}
