// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IStateMachineGrainResolver"/> addressing the grain by its class, so a
/// state machine needs no grain interface of its own to be refreshed.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="grainTypeResolver">The <see cref="GrainTypeResolver"/> giving the grain type of a grain class.</param>
public class StateMachineGrainResolver(IGrainFactory grainFactory, GrainTypeResolver grainTypeResolver) : IStateMachineGrainResolver
{
    /// <inheritdoc/>
    public IEventSourcedStateMachine Get(Type stateMachineType, StateMachineKey key) =>
        grainFactory.GetGrain<IEventSourcedStateMachine>(GrainId.Create(grainTypeResolver.GetGrainType(stateMachineType), key.ToString()));
}
