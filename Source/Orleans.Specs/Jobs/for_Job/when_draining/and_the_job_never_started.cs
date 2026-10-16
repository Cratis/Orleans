// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_the_job_never_started : given.the_job
{
    JobDrainStatus _result;

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_be_drained() => _result.ShouldEqual(JobDrainStatus.Drained);
    [Fact] void should_not_write() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Writes.ShouldEqual(0);
}
