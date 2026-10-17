// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job with one step that records the generation each start carries.
/// </summary>
public class a_job_whose_steps_report_the_generation_they_are_started_in : the_job
{
    protected JobStepId _stepId;
    protected Mock<ISomeJobStep> _step;
    protected List<long?> _startedIn = [];

    void Establish()
    {
        _stepId = JobStepId.New();
        _step = AddJobStep(_stepId);
        _step.Setup(_ => _.Start(It.IsAny<GrainId>())).Returns(() =>
        {
            _startedIn.Add(RequestContext.Get("Cratis.Orleans.Jobs.Generation") as long?);
            return Task.FromResult(Result.Failed(StartJobStepError.AlreadyStarted));
        });
        _job.ShouldBeResumable = true;
    }

    protected void WithStatusHistory(params JobStatus[] statuses)
    {
        foreach (var status in statuses)
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = statuses[^1];
        _job.CurrentState.Progress.TotalSteps = 1;
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
    }
}
