// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_discovering_key_providers : Specification
{
    IEnumerable<Type> _providers;

    void Because() => _providers = new Types.Types().FindMultiple<IJobsManagerKeys>();

    [Fact] void should_discover_the_seen_keys_catalog() => _providers.ShouldContain(typeof(JobsManagerKeys));
    [Fact] void should_not_discover_the_internal_retry_adapter_as_a_host_catalog() => _providers.Where(type => type.DeclaringType == typeof(JobsManagerKeyProviders)).ShouldBeEmpty();
}
