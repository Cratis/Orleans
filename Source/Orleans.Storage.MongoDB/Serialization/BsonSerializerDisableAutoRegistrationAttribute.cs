// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.Serialization;

/// <summary>
/// Marker attribute to tell the automatic serializer registration to not register a serializer.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BsonSerializerDisableAutoRegistrationAttribute() : Attribute;
