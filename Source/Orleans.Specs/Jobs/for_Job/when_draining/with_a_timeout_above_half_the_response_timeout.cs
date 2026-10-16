// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class with_a_timeout_above_half_the_response_timeout : given.a_running_job_with_two_steps
{
    readonly List<TimeSpan> _timeouts = [];

    void Establish()
    {
        foreach (var step in new[] { _first, _second })
        {
            step.Setup(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>())).Returns((long _, TimeSpan timeout) =>
            {
                _timeouts.Add(timeout);
                return Task.FromResult(new JobStepDrainResult(true, JobStepStatus.Stopped, Stopped));
            });
        }
    }

    async Task Because() => await _job.StopAndDrain(TimeSpan.FromMinutes(10));

    // The default Orleans response timeout is 30 seconds: half of it less a tenth.
    [Fact] void should_wait_at_most_well_within_the_response_timeout() => _timeouts.ShouldEachConformTo(_ => _ <= TimeSpan.FromSeconds(12));
    [Fact] void should_wait() => _timeouts.ShouldEachConformTo(_ => _ > TimeSpan.Zero);
}
