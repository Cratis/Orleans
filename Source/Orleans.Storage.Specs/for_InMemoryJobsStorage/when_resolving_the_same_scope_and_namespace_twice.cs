// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.for_InMemoryJobsStorage;

public class when_resolving_the_same_scope_and_namespace_twice : Specification
{
    InMemoryJobsStorage _storage;
    JobsStorage _first;
    JobsStorage _second;

    void Establish() => _storage = new(Substitute.For<IJobTypes>());

    void Because()
    {
        _first = _storage.GetFor("scope", "namespace");
        _second = _storage.GetFor("scope", "namespace");
    }

    [Fact] void should_return_the_same_instance() => _second.ShouldEqual(_first);
}
