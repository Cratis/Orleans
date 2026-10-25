// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_SiloMetadataJobStepParallelism;

public class when_the_value_is_zero : given.silo_metadata
{
    int _result;

    void Because() => _result = ParallelismOf(MetadataWith((Key, "0"))).GetMaxParallelSteps();

    [Fact] void should_fall_back_to_the_configured_limit() => _result.ShouldEqual(7);
}
