// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_nothing : given.a_silo_builder
{
    [Fact] void should_register_nothing() => _services.ShouldBeEmpty();
}
