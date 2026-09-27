// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Registers the Chronicle artifacts of event-sourced state machines: the projection maintaining each model, unless
/// Chronicle already has it, and one fluent reactor per state machine.
/// </summary>
internal static class EventSourcedStateMachineRegistrar
{
    /// <summary>
    /// Register the artifacts for every state machine the <see cref="IEventSourcedStateMachineDefinitions"/> in a
    /// service provider knows.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> holding the definitions and the reactors' collaborators.</param>
    /// <param name="registrations">The <see cref="IStateMachineArtifactRegistrations"/> to register on.</param>
    /// <remarks>
    /// The reactors resolve their collaborators from <paramref name="services"/> on first delivery rather than now,
    /// because the Chronicle client is typically created before the silo - and its grain factory - has started.
    /// </remarks>
    public static void Register(IServiceProvider services, IStateMachineArtifactRegistrations registrations)
    {
        var options = services.GetRequiredService<IOptions<EventSourcedStateMachinesOptions>>().Value;
        Register(
            services.GetRequiredService<IEventSourcedStateMachineDefinitions>().All,
            registrations,
            definition => new(
                definition,
                services.GetRequiredService<IStateMachineGrainResolver>,
                services.GetRequiredService<IStateMachineEventStores>,
                options));
    }

    /// <summary>
    /// Register the artifacts for state machines.
    /// </summary>
    /// <param name="definitions">The <see cref="EventSourcedStateMachineDefinition"/> of each state machine.</param>
    /// <param name="registrations">The <see cref="IStateMachineArtifactRegistrations"/> to register on.</param>
    /// <param name="createReactor">Creates the <see cref="EventSourcedStateMachineReactor"/> for a state machine.</param>
    public static void Register(
        IEnumerable<EventSourcedStateMachineDefinition> definitions,
        IStateMachineArtifactRegistrations registrations,
        Func<EventSourcedStateMachineDefinition, EventSourcedStateMachineReactor> createReactor)
    {
        foreach (var definition in definitions)
        {
            definition.RegisterProjection?.Invoke(registrations);
            registrations.RegisterReactor(definition.ReactorId, createReactor(definition).Define);
        }
    }
}
