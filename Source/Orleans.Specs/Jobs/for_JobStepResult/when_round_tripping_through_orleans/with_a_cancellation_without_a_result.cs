// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Orleans.Jobs.for_JobStepResult.when_round_tripping_through_orleans;

public class with_a_cancellation_without_a_result : Specification
{
    ServiceProvider _provider;
    Serializer _serializer;
    JobStepResult _original;
    PerformJobStepError _error;
    Dictionary<string, int> _result;
    bool _hasFullResult;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddCratisOrleansSerializers();
        _provider = services.BuildServiceProvider();
        _serializer = _provider.GetRequiredService<Serializer>();
        _original = JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
    }

    void Because()
    {
        var roundTripped = _serializer.Deserialize<JobStepResult>(_serializer.SerializeToArray(_original)!);
        _hasFullResult = roundTripped.TryGetFullResult(out _result, out _error, new JsonSerializerOptions());
    }

    [Fact] void should_not_have_a_full_result() => _hasFullResult.ShouldBeFalse();
    [Fact] void should_have_no_result() => _result.ShouldBeNull();
    [Fact] void should_preserve_the_null_partial_result() => _error.PartialResult.ShouldBeNull();
    [Fact] void should_have_no_partial_result() => _error.HasPartialResult.ShouldBeFalse();
    [Fact] void should_remain_cancelled() => _error.Cancelled.ShouldBeTrue();
    [Fact] void should_preserve_the_cancellation_message() => _error.ErrorMessages.ShouldContainOnly("Job step task was cancelled");

    void Destroy() => _provider.Dispose();
}
