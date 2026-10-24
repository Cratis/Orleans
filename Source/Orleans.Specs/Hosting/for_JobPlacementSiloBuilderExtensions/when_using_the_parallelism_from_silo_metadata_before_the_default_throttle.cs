// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_using_the_parallelism_from_silo_metadata_before_the_default_throttle : given.a_silo_with_metadata_parallelism
{
    void Because()
    {
        _silo.UseJobStepParallelismFromSiloMetadata();
        AddTheDefaultThrottle(_services);
        _throttle = _services.BuildServiceProvider().GetRequiredService<IJobStepThrottle>();
    }

    [Fact] void should_limit_the_silo_to_the_metadata_value() => GrantsOnly(_throttle, 2).ShouldBeTrue();
}
