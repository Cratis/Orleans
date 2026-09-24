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

    static readonly object _registerLock = new();
    static bool _isRegistered;

    /// <summary>
    /// Registers the convention pack, once per process.
    /// </summary>
    /// <remarks>
    /// Implementing <see cref="ICanProvideMongoDBConventionPacks"/> only offers the pack to a host that goes
    /// looking for it. Nothing in this package did, so in any host that did not happen to discover it the
    /// job state serialized with Pascal-case element names while every filter in the storage was written
    /// against camel case - including the composite <c lang="csharp">_id</c>. No filter matched: reads came
    /// back empty and upserts were rejected for altering an immutable <c lang="csharp">_id</c>.
    /// The provider now registers its own conventions, so the documents it writes and the filters it queries
    /// them with cannot disagree.
    /// The lock is an <see cref="object"/> rather than a <c lang="csharp">System.Threading.Lock</c>, because
    /// this package still targets net8.0 where that type does not exist.
    /// </remarks>
    public static void EnsureRegistered()
    {
        lock (_registerLock)
        {
            if (_isRegistered)
            {
                return;
            }

            new ConventionPacks().Provide();
            _isRegistered = true;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        var predicate = new Func<Type, bool>(type => _namespaces.Any(n => type.Namespace?.StartsWith(n) == true));
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(predicate),

            // Job and job step state are read back as their base types, but what was written is a consumer's
            // own subclass carrying its own properties. Without this, the first such property makes the read
            // throw rather than return the state - so a job authored the way the documentation shows cannot
            // have its steps read at all.
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("CamelCase", conventionPack, predicate);
        return [new("CamelCase", conventionPack)];
    }
}
