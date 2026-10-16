// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping;

public class with_a_known_job : given.jobs
{
    StopJobResult _result;
    async Task Because() => _result = await _jobs.Stop(_key, _id);

    [Fact] void should_report_the_stopped_identity() => _result.ShouldEqual(new StopJobResult.Stopped(_id));
    [Fact] void should_stop_the_job() => _job.Received(1).Stop();
    [Fact] void should_not_deserialize_the_request() => _storage.DidNotReceive().GetJob(Arg.Any<JobId>());
}
