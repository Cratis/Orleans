// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A running job with two live steps, whose drain answers each specification arranges.
/// </summary>
public class a_running_job_with_two_steps : the_job
{
    protected JobStepId _firstId;
    protected JobStepId _secondId;
    protected Mock<ISomeJobStep> _first;
    protected Mock<ISomeJobStep> _second;

    protected int Writes => _silo.StorageStats<SomeJob, SomeJobState>().Writes;

    async Task Establish()
    {
        _firstId = JobStepId.New();
        _secondId = JobStepId.New();
        _first = AddJobStep(_firstId);
        _second = AddJobStep(_secondId);
        foreach (var step in new[] { _first, _second })
        {
            step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
        }
        await _job.Start(new());
        StoredJobStep(_firstId).Status = JobStepStatus.Running;
        StoredJobStep(_secondId).Status = JobStepStatus.Running;
    }
}
