// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class with_a_supplied_identity : given.jobs
{
    Result<JobId, StartJobError> _result;
    async Task Because() => _result = await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request);

    [Fact] void should_return_the_started_identity() => _result.AsT0.ShouldEqual(_id);
    [Fact] void should_start_the_job_directly() => _job.Received(1).Start(_request);
    [Fact] void should_not_start_through_the_manager() => _manager.DidNotReceive().Start<INullJobWithSomeRequest, SomeJobRequest>(Arg.Any<JobId>(), Arg.Any<SomeJobRequest>());
    [Fact] async Task should_remember_the_key() => (await _keys.GetKeys(CancellationToken.None)).ShouldContain(new JobsManagerKey(_key.Scope, _key.Namespace));
    [Fact] void should_activate_the_manager_with_a_bounded_query() => _manager.Received(1).GetJobs(Arg.Is<Storage.Jobs.JobQuery>(query => query.Take == 1));
    [Fact] void should_not_rehydrate_during_preparation() => _manager.DidNotReceive().Rehydrate();
}
