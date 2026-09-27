// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when a <see cref="ReadModelStorage{TModel}"/> is read before it has been bound to its grain.
/// </summary>
/// <param name="modelType">The type of model the storage holds.</param>
public class ReadModelStorageIsNotBound(Type modelType)
    : Exception($"The read model storage for '{modelType.FullName}' is read before it was bound to its grain - it is bound when the state machine activates");
