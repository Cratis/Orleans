// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_PerformJobStepError;

public class when_creating_a_cancellation_without_a_result : Specification
{
    PerformJobStepError _error;

    void Because() => _error = PerformJobStepError.CancelledWithNoResult();

    [Fact] void should_have_no_partial_result() => _error.HasPartialResult.ShouldBeFalse();
    [Fact] void should_store_no_partial_result() => _error.PartialResult.ShouldBeNull();
    [Fact] void should_not_retrieve_a_string_partial_result() => _error.TryGetPartialResult<string>(out _).ShouldBeFalse();
    [Fact] void should_not_retrieve_an_object_partial_result() => _error.TryGetPartialResult<object>(out _).ShouldBeFalse();
    [Fact] void should_be_cancelled() => _error.Cancelled.ShouldBeTrue();
    [Fact] void should_keep_the_cancellation_message() => _error.ErrorMessages.ShouldContainOnly("Job step task was cancelled");
}
