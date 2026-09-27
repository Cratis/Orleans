// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.Integration;

/// <summary>
/// Puts every state machine integration spec in one collection, so they run one at a time against the shared cluster.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class StateMachineClusterCollection
{
    public const string Name = "StateMachineCluster";
}
