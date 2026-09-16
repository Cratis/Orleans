// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Provides MongoDB convention packs.
/// </summary>
public class ConventionPacks : ICanProvideMongoDBConventionPacks
{
    static readonly string[] _namespaces =
    [
        "Cratis.Orleans.Storage",
        "Cratis.Orleans.Jobs"
    ];

    /// <inheritdoc/>
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        var predicate = new Func<Type, bool>(type => _namespaces.Any(n => type.Namespace?.StartsWith(n) == true));
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(predicate)
        };
        ConventionRegistry.Register("CamelCase", conventionPack, predicate);
        return [new("CamelCase", conventionPack)];
    }
}
