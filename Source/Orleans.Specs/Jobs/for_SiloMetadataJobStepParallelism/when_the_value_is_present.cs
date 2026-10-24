// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_SiloMetadataJobStepParallelism;

public class when_the_value_is_present : given.silo_metadata
{
    int _result;

    void Because() => _result = ParallelismOf(MetadataWith((Key, "3"))).GetMaxParallelSteps();

    [Fact] void should_use_the_metadata_value() => _result.ShouldEqual(3);
}
