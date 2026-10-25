// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_using_the_parallelism_from_silo_metadata_with_another_key : given.a_silo_with_metadata_parallelism
{
    void Because()
    {
        _silo.UseJobStepParallelismFromSiloMetadata("some.other.key");
        _throttle = _services.BuildServiceProvider().GetRequiredService<IJobStepThrottle>();
    }

    [Fact] void should_fall_back_to_the_configured_limit_when_the_key_is_missing() => GrantsOnly(_throttle, 5).ShouldBeTrue();
}
