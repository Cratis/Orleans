// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Types;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IEventSourcedStateMachineDefinitions"/> that inspects the state machine
/// types registered in <see cref="EventSourcedStateMachinesOptions"/> and, when enabled, every concrete
/// <see cref="EventSourcedStateMachine{TModel}"/> type discovery finds.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to discover state machines through.</param>
/// <param name="artifacts">The <see cref="IClientArtifactsProvider"/> with what Chronicle has discovered.</param>
/// <param name="options">The <see cref="EventSourcedStateMachinesOptions"/>.</param>
public class EventSourcedStateMachineDefinitions(
    ITypes types,
    IClientArtifactsProvider artifacts,
    IOptions<EventSourcedStateMachinesOptions> options) : IEventSourcedStateMachineDefinitions
{
    readonly Lazy<IReadOnlyList<EventSourcedStateMachineDefinition>> _definitions = new(() =>
    [
        .. (options.Value.Discover ? types.All.Where(EventSourcedStateMachineInspector.IsEventSourcedStateMachine) : [])
            .Concat(options.Value.StateMachines)
            .Distinct()
            .Select(type => EventSourcedStateMachineInspector.Inspect(type, artifacts))
    ]);

    /// <inheritdoc/>
    public IEnumerable<EventSourcedStateMachineDefinition> All => _definitions.Value;
}
