// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_PerformJobStepError.when_creating_a_failure_with_a_partial_result;

public class with_an_empty_string : Specification
{
    PerformJobStepError _error;

    void Because() => _error = PerformJobStepError.FailedWithPartialResult(string.Empty, ["step failed"], string.Empty);

    [Fact] void should_have_a_partial_result() => _error.HasPartialResult.ShouldBeTrue();
    [Fact] void should_retrieve_the_string_partial_result() => _error.TryGetPartialResult<string>(out _).ShouldBeTrue();
    [Fact] void should_keep_the_empty_string() => _error.PartialResult.ShouldEqual(string.Empty);
    [Fact] void should_not_be_cancelled() => _error.Cancelled.ShouldBeFalse();
}
