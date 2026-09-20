// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Orleans.Serialization.for_CratisJsonSerializer.given;

/// <summary>
/// The arrangement that broke production: the Cratis serializers registered first, then a host
/// registering Orleans' shared JSON codec for its own namespaces with its own options - which
/// overwrites the shared codec's single <c language="csharp">JsonCodecOptions.SerializerOptions</c>. The Cratis types
/// must be unaffected by that overwrite.
/// </summary>
public class a_serializer_with_a_host_json_serializer : Specification
{
    protected Serializer _serializer;
    protected DeepCopier _copier;
    protected JobTypes _jobTypes;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IJob>().Returns([typeof(NullJobWithSomeRequest)]);
        _jobTypes = new JobTypes(types);

        var services = new ServiceCollection();
        services.AddCratisOrleansSerializers();

        services.AddSerializer(builder => builder.AddJsonSerializer(
            type => type.Namespace?.StartsWith("SomeHost", StringComparison.Ordinal) ?? false,
            new JsonSerializerOptions()));

        var provider = services.BuildServiceProvider();
        _serializer = provider.GetRequiredService<Serializer>();
        _copier = provider.GetRequiredService<DeepCopier>();
    }

    protected T RoundTrip<T>(T value) => _serializer.Deserialize<T>(_serializer.SerializeToArray(value)!)!;
}
