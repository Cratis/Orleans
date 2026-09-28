// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Orleans.Chronicle.StateMachines;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Hosting;

/// <summary>
/// Extension methods for adding event-sourced state machines to an application.
/// </summary>
public static class EventSourcedStateMachinesServiceCollectionExtensions
{
    /// <summary>
    /// Add event-sourced state machines: every concrete <see cref="EventSourcedStateMachine{TModel}"/> is inspected, and
    /// its projection and reactor are registered with the Chronicle client when it is created.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Optional callback for configuring the <see cref="EventSourcedStateMachinesOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The registration hooks into the <see cref="ChronicleOptions"/> the client is created from - whichever type of
    /// options the hosting uses - so it must be in place before the client is first resolved. A state machine that cannot
    /// be inspected fails client creation, rather than leaving a state machine that silently never refreshes.
    /// </remarks>
    public static IServiceCollection AddEventSourcedStateMachines(this IServiceCollection services, Action<EventSourcedStateMachinesOptions>? configure = default)
    {
        services.AddOptions<EventSourcedStateMachinesOptions>().Configure(options => configure?.Invoke(options));
        services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        services.TryAddSingleton<IClientArtifactsProvider>(DefaultClientArtifactsProvider.Default);
        services.TryAddSingleton<IEventSourcedStateMachineDefinitions, EventSourcedStateMachineDefinitions>();
        services.TryAddSingleton<IStateMachineEventStores, StateMachineEventStores>();
        services.TryAddSingleton<IStateMachineGrainResolver, StateMachineGrainResolver>();

        // Open generic, constrained to ChronicleOptions: the container only applies it to the options types that
        // derive from it - ChronicleOptions, ChronicleClientOptions, the ASP.NET Core options - and skips the rest.
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IConfigureOptions<>), typeof(RegisterEventSourcedStateMachines<>)));
        return services;
    }

    /// <summary>
    /// Add event-sourced state machines to the silo's services.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/>.</param>
    /// <param name="configure">Optional callback for configuring the <see cref="EventSourcedStateMachinesOptions"/>.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddEventSourcedStateMachines(this ISiloBuilder builder, Action<EventSourcedStateMachinesOptions>? configure = default)
    {
        builder.Services.AddEventSourcedStateMachines(configure);
        return builder;
    }
}
