// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_deleting;

public class with_a_known_job : given.jobs
{
    DeleteJobResult _result;
    async Task Because() => _result = await _jobs.Delete(_key, _id);

    [Fact] void should_report_the_removed_identity() => _result.ShouldEqual(new DeleteJobResult.Deleted(_id));
    [Fact] void should_remove_through_the_job() => _job.Received(1).Remove();
    [Fact] void should_not_remove_state_directly() => _storage.DidNotReceive().Remove(Arg.Any<JobId>());
}
