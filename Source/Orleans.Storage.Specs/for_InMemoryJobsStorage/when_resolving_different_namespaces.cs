// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.for_InMemoryJobsStorage;

public class when_resolving_different_namespaces : Specification
{
    InMemoryJobsStorage _storage;
    JobsStorage _first;
    JobsStorage _second;

    void Establish() => _storage = new(Substitute.For<IJobTypes>());

    void Because()
    {
        _first = _storage.GetFor("scope", "one");
        _second = _storage.GetFor("scope", "two");
    }

    [Fact] void should_return_different_instances() => _second.ShouldNotEqual(_first);
}
