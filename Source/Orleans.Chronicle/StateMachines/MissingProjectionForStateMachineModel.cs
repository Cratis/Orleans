// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when nothing defines the projection maintaining an event-sourced state machine's model.
/// </summary>
/// <param name="stateMachineType">The type of state machine.</param>
/// <param name="modelType">The type of model.</param>
public class MissingProjectionForStateMachineModel(Type stateMachineType, Type modelType)
    : Exception($"Nothing defines the projection maintaining '{modelType.FullName}' for the state machine '{stateMachineType.FullName}' - override DefineProjection, or put model-bound projection attributes on the model");
