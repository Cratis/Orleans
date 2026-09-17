// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsOptions;

public class with_explicit_max_parallel_steps : Specification
{
    JobsOptions _options;

    void Establish() => _options = new() { MaxParallelSteps = 5 };

    [Fact] void should_use_the_configured_value() => _options.GetEffectiveMaxParallelSteps().ShouldEqual(5);
}
