// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class without_a_supplied_identity : given.jobs
{
    Result<JobId, StartJobError> _result;
    void Establish() => _factory.GetGrain<INullJobWithSomeRequest>(Arg.Any<Guid>(), _key).Returns(_job);
    async Task Because() => _result = await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _request);

    [Fact] void should_generate_an_identity() => _result.AsT0.Value.ShouldNotEqual(Guid.Empty);
    [Fact] void should_use_the_generated_identity() => _factory.Received(1).GetGrain<INullJobWithSomeRequest>(_result.AsT0, _key);
}
