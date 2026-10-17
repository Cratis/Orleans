// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.given;

public class an_interrupted_job_rejecting_resume : a_job_left_running
{
    void Establish()
    {
        _job.CurrentState.Request = new SomeRequest();
        _job.CurrentState.StatusChanges.Insert(0, new() { Status = JobStatus.PreparingJob });
        _job.ShouldBeResumable = true;
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
    }
}
