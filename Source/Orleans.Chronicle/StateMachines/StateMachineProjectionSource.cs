// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents where the projection maintaining an event-sourced state machine's model comes from.
/// </summary>
public enum StateMachineProjectionSource
{
    /// <summary>
    /// The state machine describes the projection in <see cref="EventSourcedStateMachine{TModel}"/>'s <c language="csharp">DefineProjection</c>,
    /// and the integration registers it.
    /// </summary>
    DefinedByStateMachine = 0,

    /// <summary>
    /// The model carries model-bound projection attributes that Chronicle has not discovered, and the integration
    /// registers them.
    /// </summary>
    ModelBound = 1,

    /// <summary>
    /// Chronicle already has the projection - model-bound attributes or an <c language="csharp">IProjectionFor&lt;TModel&gt;</c>
    /// it discovered - and the integration registers nothing.
    /// </summary>
    AlreadyDiscovered = 2
}
