// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_SiloMetadataJobStepParallelism;

public class when_another_key_is_used : given.silo_metadata
{
    int _result;

    void Because() => _result = ParallelismOf(MetadataWith((Key, "3"), ("some.other.key", "5"))).GetMaxParallelSteps("some.other.key");

    [Fact] void should_use_the_value_of_that_key() => _result.ShouldEqual(5);
}
