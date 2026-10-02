// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsOptions;

public class with_default_values : Specification
{
    JobsOptions _options;

    void Because() => _options = new JobsOptions();

    [Fact] void should_limit_rehydration_to_eight() => _options.MaxConcurrentRehydration.ShouldEqual(8);
    [Fact] void should_limit_cleanup_to_four() => _options.MaxConcurrentCleanup.ShouldEqual(4);
    [Fact] void should_limit_step_starts_to_sixteen() => _options.MaxConcurrentStepStarts.ShouldEqual(16);
}
