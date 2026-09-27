// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Represents the <see cref="IConfigureOptions{TOptions}"/> that registers the Chronicle artifacts of every event-sourced
/// state machine on the explicit artifacts of any <see cref="ChronicleOptions"/> the Chronicle client is created from.
/// </summary>
/// <param name="services">The <see cref="IServiceProvider"/> holding the state machine definitions, which the reactors also resolve their collaborators from on first delivery.</param>
/// <typeparam name="TOptions">Type of <see cref="ChronicleOptions"/>.</typeparam>
/// <remarks>
/// Explicit artifacts are given to every event store the client creates, whatever its namespace, and are registered
/// again whenever the connection is re-established - which is what makes the state machines follow every tenant. The
/// reactors resolve the grain factory lazily, because the client is typically created before the silo has started.
/// </remarks>
internal sealed class RegisterEventSourcedStateMachines<TOptions>(IServiceProvider services) : IConfigureOptions<TOptions>
    where TOptions : ChronicleOptions
{
    static readonly ConditionalWeakTable<ExplicitArtifacts, object> _registered = [];

    /// <inheritdoc/>
    public void Configure(TOptions options)
    {
        if (_registered.TryAdd(options.ExplicitArtifacts, new()))
        {
            EventSourcedStateMachineRegistrar.Register(services, new ExplicitArtifactsRegistrations(options.ExplicitArtifacts));
        }
    }
}
