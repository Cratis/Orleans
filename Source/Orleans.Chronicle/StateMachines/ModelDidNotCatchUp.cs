// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown when a materialized state machine model has not caught up with a delivered event within
/// the configured time. Failing the delivery keeps a stale model from deciding a transition; Chronicle retries the
/// failed partition.
/// </summary>
/// <param name="modelType">The type of model.</param>
/// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the delivered event.</param>
/// <param name="timeout">How long it waited.</param>
public class ModelDidNotCatchUp(Type modelType, EventSequenceNumber sequenceNumber, TimeSpan timeout)
    : Exception($"The model '{modelType.FullName}' did not catch up with the event at sequence number {sequenceNumber} within {timeout} - prefer a passive model for state machines");
