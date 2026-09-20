// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Json;
using Cratis.Orleans.Jobs;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Orleans.Serialization;

/// <summary>
/// Represents the JSON serializer that owns every Cratis type crossing a grain boundary without a
/// codec of its own - concepts inside plain records, job states and the other Cratis-owned shapes
/// that have no generated Orleans codec.
/// </summary>
/// <remarks>
/// A dedicated codec rather than a claim on Orleans' shared <c language="csharp">JsonCodec</c>, deliberately. The shared
/// codec is a single instance with a single <c language="csharp">JsonCodecOptions.SerializerOptions</c>: every
/// <c language="csharp">AddJsonSerializer</c> call adds its type predicate additively but <em>overwrites</em> the options,
/// so the last registration wins for every type the shared codec handles. A host registering a JSON
/// serializer for its own namespaces - the arrangement this package itself recommends - silently
/// replaced the options carrying <see cref="JobStateConverter"/>, and every
/// <c language="csharp">IJobsManager.GetJobsOfType</c> call then failed deep-copying
/// <c language="csharp">JobState.Request</c>, an interface-typed property only that converter can read back. Owning the
/// codec end to end means no host configuration can reach the serialization of Cratis types.
/// <para>
/// The wire format matches Orleans' own JSON codec: the concrete type (field 0) followed by the JSON
/// payload (field 1), so the value's runtime type round-trips across silos. Deep copies within a silo
/// go through the same serialize-deserialize pair.
/// </para>
/// </remarks>
public class CratisJsonSerializer : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    static readonly Type _codecType = typeof(CratisJsonSerializer);
    readonly JsonSerializerOptions _options = CreateSerializerOptions();

    /// <summary>
    /// Creates the <see cref="JsonSerializerOptions"/> the Cratis types are serialized with - camel-cased,
    /// with concepts and job states handled by their converters.
    /// </summary>
    /// <returns>The <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        options.Converters.Add(new JobStateConverter());
        return options;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] object? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        // The schema type on the wire is this codec's own type, so the reading side resolves this codec
        // regardless of what the value's declared type was - the value's runtime type travels as field 0.
        writer.WriteFieldHeader(fieldIdDelta, expectedType, _codecType, WireType.TagDelimited);

        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeaderExpected(0, WireType.LengthPrefixed);
        writer.Session.TypeCodec.WriteLengthPrefixed(ref writer, value.GetType());

        var bufferWriter = new BufferWriterBox<PooledBuffer>(new PooledBuffer());
        try
        {
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);
            JsonSerializer.Serialize(jsonWriter, value, _options);
            jsonWriter.Flush();

            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeaderExpected(1, WireType.LengthPrefixed);
            writer.WriteVarUInt32((uint)bufferWriter.Value.Length);
            bufferWriter.Value.CopyTo(ref writer);
        }
        finally
        {
            bufferWriter.Value.Dispose();
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public object? ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.IsReference)
        {
            return ReferenceCodec.ReadReference(ref reader, field.FieldType);
        }

        field.EnsureWireTypeTagDelimited();

        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
        object? result = null;
        Type? type = null;
        uint fieldId = 0;
        while (true)
        {
            var header = reader.ReadFieldHeader();
            if (header.IsEndBaseOrEndObject)
            {
                break;
            }

            fieldId += header.FieldIdDelta;
            switch (fieldId)
            {
                case 0:
                    ReferenceCodec.MarkValueField(reader.Session);
                    type = reader.Session.TypeCodec.ReadLengthPrefixed(ref reader);
                    break;

                case 1:
                    if (type is null)
                    {
                        throw new RequiredFieldMissingException("Serialized value is missing its type field.");
                    }

                    ReferenceCodec.MarkValueField(reader.Session);
                    var length = reader.ReadVarUInt32();

                    var tempBuffer = new PooledBuffer();
                    try
                    {
                        reader.ReadBytes(ref tempBuffer, (int)length);
                        var sequence = tempBuffer.AsReadOnlySequence();
                        var jsonReader = new Utf8JsonReader(sequence);
                        result = typeof(JsonNode).IsAssignableFrom(type)
                            ? JsonNode.Parse(ref jsonReader)
                            : JsonSerializer.Deserialize(ref jsonReader, type, _options);
                    }
                    finally
                    {
                        tempBuffer.Dispose();
                    }

                    break;

                default:
                    reader.ConsumeUnknownField(header);
                    break;
            }
        }

        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
        return result;
    }

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(input))]
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (context.TryGetCopy(input, out object? result))
        {
            return result;
        }

        if (input is JsonNode jsonNode)
        {
            var clone = jsonNode.DeepClone();
            context.RecordCopy(input, clone);
            return clone;
        }

        var bufferWriter = new BufferWriterBox<PooledBuffer>(new PooledBuffer());
        try
        {
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);
            JsonSerializer.Serialize(jsonWriter, input!, _options);
            jsonWriter.Flush();

            var sequence = bufferWriter.Value.AsReadOnlySequence();
            var jsonReader = new Utf8JsonReader(sequence);
            result = JsonSerializer.Deserialize(ref jsonReader, input!.GetType(), _options)
                ?? throw new JsonException($"System.Text.Json returned null while deep copying an instance of type '{input.GetType()}'.");
        }
        finally
        {
            bufferWriter.Value.Dispose();
        }

        context.RecordCopy(input!, result);
        return result;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == _codecType || IsCratisOwned(type);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) => IsCratisOwned(type) ? true : null;

    /// <summary>
    /// Whether a type belongs to the set this serializer owns: the Cratis namespaces, the OneOf marker
    /// types Cratis payloads embed, and <see cref="JsonObject"/> - the same set that used to be claimed
    /// on the shared codec. <c language="csharp">OneOfBase</c> derivatives are excluded; <see cref="OneOfSerializer"/> owns
    /// those, and abstract types and interfaces carry no payload of their own - the runtime type is what
    /// gets serialized.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> when this serializer owns the type.</returns>
    static bool IsCratisOwned(Type type)
    {
        if (type == typeof(JsonObject))
        {
            return true;
        }

        if (CommonCodecTypeFilter.IsAbstractOrFrameworkType(type))
        {
            return false;
        }

        if (IsOneOfDerivative(type))
        {
            return false;
        }

        return type.Namespace == "OneOf.Types"
            || (type.Namespace?.StartsWith("Cratis", StringComparison.Ordinal) ?? false);
    }

    static bool IsOneOfDerivative(Type type)
    {
        var current = type;
        while (current != typeof(object) && current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.Contains("OneOfBase"))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
