// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Defines where the integration registers the Chronicle artifacts of event-sourced state machines.
/// </summary>
public interface IStateMachineArtifactRegistrations
{
    /// <summary>
    /// Register a projection defined in code.
    /// </summary>
    /// <typeparam name="TModel">Type of read model.</typeparam>
    /// <param name="define">The definition of the projection.</param>
    void RegisterProjection<TModel>(Action<IProjectionBuilderFor<TModel>> define);

    /// <summary>
    /// Register a read model whose projection is declared by its model-bound attributes.
    /// </summary>
    /// <typeparam name="TModel">Type of read model.</typeparam>
    void RegisterReadModel<TModel>();

    /// <summary>
    /// Register a reactor defined fluently.
    /// </summary>
    /// <param name="id">The <see cref="ReactorId"/>.</param>
    /// <param name="define">The definition of the reactor.</param>
    void RegisterReactor(ReactorId id, Action<IReactorBuilder> define);
}
