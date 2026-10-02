// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobTypes;

public class when_registering_a_named_job : Specification
{
    JobTypes _registry;
    ITypes _types;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<IJob>().Returns([typeof(NamedJob)]);
    }

    void Because() => _registry = new JobTypes(_types);

    [Fact] void should_enumerate_the_persisted_name_rather_than_the_clr_name() => _registry.All.ShouldContainOnly(new JobType("custom-stored-name"));
    [Fact] void should_resolve_every_enumerated_type() => _registry.All.All(type => _registry.GetClrTypeFor(type).IsSuccess).ShouldBeTrue();

    [JobType("custom-stored-name")]
    public class NamedJob : NullJobWithSomeRequest;
}
