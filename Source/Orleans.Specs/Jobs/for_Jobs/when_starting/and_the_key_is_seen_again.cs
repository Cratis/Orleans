// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_starting;

public class and_the_key_is_seen_again : given.jobs
{
    async Task Establish() => await _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request);
    Task Because() => _jobs.Start<INullJobWithSomeRequest, SomeJobRequest>(_key, _id, _request);

    [Fact] void should_only_touch_the_manager_once() => _manager.Received(1).GetJobs(Arg.Any<Storage.Jobs.JobQuery>());
    [Fact] void should_still_call_the_job_for_each_start() => _job.Received(2).Start(_request);
}
