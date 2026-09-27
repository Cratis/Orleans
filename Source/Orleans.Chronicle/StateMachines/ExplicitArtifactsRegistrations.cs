// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IStateMachineArtifactRegistrations"/> that registers on the Chronicle
/// client's <see cref="ExplicitArtifacts"/>, so every event store the client creates - whatever its namespace - gets them.
/// </summary>
/// <param name="artifacts">The <see cref="ExplicitArtifacts"/> to register on.</param>
internal sealed class ExplicitArtifactsRegistrations(ExplicitArtifacts artifacts) : IStateMachineArtifactRegistrations
{
    /// <inheritdoc/>
    public void RegisterProjection<TModel>(Action<IProjectionBuilderFor<TModel>> define) => artifacts.RegisterProjection(define);

    /// <inheritdoc/>
    public void RegisterReadModel<TModel>() => artifacts.RegisterReadModel<TModel>();

    /// <inheritdoc/>
    public void RegisterReactor(ReactorId id, Action<IReactorBuilder> define) => artifacts.RegisterReactor(id, define);
}
