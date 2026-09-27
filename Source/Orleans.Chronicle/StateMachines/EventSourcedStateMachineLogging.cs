// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Chronicle.StateMachines;

internal static partial class EventSourcedStateMachineLogging
{
    [LoggerMessage(LogLevel.Debug, "Transition to '{TargetState}' for event '{EventType}' is not taken - its guards are not satisfied by the model")]
    internal static partial void GuardNotSatisfied(this ILogger logger, Type eventType, Type targetState);

    [LoggerMessage(LogLevel.Debug, "Transition to '{TargetState}' for event '{EventType}' is not taken - the current state does not allow it")]
    internal static partial void CurrentStateDoesNotAllowTransition(this ILogger logger, Type eventType, Type targetState);
}
