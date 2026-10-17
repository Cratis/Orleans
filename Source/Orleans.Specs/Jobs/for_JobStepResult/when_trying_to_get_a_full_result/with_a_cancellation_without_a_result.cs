// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Orleans.Jobs.for_JobStepResult.when_trying_to_get_a_full_result;

public class with_a_cancellation_without_a_result : Specification
{
    JobStepResult _stepResult;
    PerformJobStepError _cancellation;
    PerformJobStepError _error;
    Dictionary<string, int> _result;
    bool _hasFullResult;
    Exception _exception;

    void Establish()
    {
        _cancellation = PerformJobStepError.CancelledWithNoResult();
        _stepResult = JobStepResult.Failed(_cancellation);
    }

    void Because() => _exception = Catch.Exception(() => _hasFullResult = _stepResult.TryGetFullResult(out _result, out _error, new JsonSerializerOptions()));

    [Fact] void should_not_throw_for_a_structured_result() => _exception.ShouldBeNull();
    [Fact] void should_not_have_a_full_result() => _hasFullResult.ShouldBeFalse();
    [Fact] void should_have_no_result() => _result.ShouldBeNull();
    [Fact] void should_return_the_cancellation() => _error.ShouldBeSame(_cancellation);
}
