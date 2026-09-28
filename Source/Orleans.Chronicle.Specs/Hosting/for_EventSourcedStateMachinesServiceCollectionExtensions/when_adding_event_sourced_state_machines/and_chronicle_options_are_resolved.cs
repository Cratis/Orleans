// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Orleans.Chronicle.Hosting.for_EventSourcedStateMachinesServiceCollectionExtensions.given;
using Cratis.Orleans.Chronicle.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Hosting.for_EventSourcedStateMachinesServiceCollectionExtensions.when_adding_event_sourced_state_machines;

public class and_chronicle_options_are_resolved : Specification
{
    IEventSourcedStateMachineDefinitions _definitions;
    ServiceProvider _serviceProvider;
    UnrelatedOptions _unrelatedOptions;

    void Establish()
    {
        _definitions = Substitute.For<IEventSourcedStateMachineDefinitions>();
        _definitions.All.Returns([]);
        var services = new ServiceCollection();
        services.AddSingleton(_definitions);
        services.AddOptions<UnrelatedOptions>().Configure(_ => _.Value = "configured");
        services.AddEventSourcedStateMachines();
        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because()
    {
        _ = _serviceProvider.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
        _ = _serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        _unrelatedOptions = _serviceProvider.GetRequiredService<IOptions<UnrelatedOptions>>().Value;
    }

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_register_the_state_machines_on_each_chronicle_options_instance() => _ = _definitions.Received(2).All;
    [Fact] void should_leave_options_that_are_not_chronicle_options_alone() => _unrelatedOptions.Value.ShouldEqual("configured");
}
