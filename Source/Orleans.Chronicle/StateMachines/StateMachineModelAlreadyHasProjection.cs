// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when an event-sourced state machine defines a projection for a model Chronicle already
/// has a projection for.
/// </summary>
/// <param name="stateMachineType">The type of state machine.</param>
/// <param name="modelType">The type of model.</param>
/// <param name="projectionType">The type of the discovered projection, or null when it is the model's own model-bound projection.</param>
public class StateMachineModelAlreadyHasProjection(Type stateMachineType, Type modelType, Type? projectionType)
    : Exception($"The state machine '{stateMachineType.FullName}' defines a projection for '{modelType.FullName}', which Chronicle already has a projection for ({projectionType?.FullName ?? "its model-bound attributes"}). A read model is maintained by one projection - remove DefineProjection to use the existing one.");
