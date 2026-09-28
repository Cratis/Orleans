// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines;
using Orleans.TestingHost;

namespace Cratis.Orleans.Chronicle.Integration;

/// <summary>
/// A real two-silo Orleans cluster hosting event-sourced state machines, with Chronicle's side - the read models and the
/// delivery of events to the registered reactors - played by <see cref="InMemoryReadModels"/> and
/// <see cref="RecordingRegistrations"/>.
/// </summary>
public class StateMachineClusterFixture : IDisposable
{
    static readonly Lazy<StateMachineClusterFixture> _shared = new(() => new StateMachineClusterFixture(), isThreadSafe: true);

    readonly TestCluster _cluster;

    public StateMachineClusterFixture()
    {
        _cluster = new TestClusterBuilder(2)
            .AddSiloBuilderConfigurator<ClusterConfigurator>()
            .AddClientBuilderConfigurator<ClusterConfigurator>()
            .Build();
        _cluster.Deploy();

        // Exactly what the Chronicle client is given when it is created: the registrations made from the silo's services.
        Registrations = new();
        EventSourcedStateMachineRegistrar.Register(((InProcessSiloHandle)_cluster.Primary).SiloHost.Services, Registrations);
    }

    public static StateMachineClusterFixture Shared => _shared.Value;

    public RecordingRegistrations Registrations { get; }

    public IGrainFactory GrainFactory => _cluster.GrainFactory;

    public void Dispose()
    {
        _cluster.StopAllSilos();
        _cluster.Dispose();
        GC.SuppressFinalize(this);
    }
}
