// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming_if_running.given;

public abstract class a_job_that_is_not_running : for_Job.given.a_job_left_running
{
    protected int _writesBefore;
    protected int _changesBefore;

    protected abstract JobStatus Status { get; }

    void Establish()
    {
        _job.CurrentState.Request = new for_Job.given.SomeRequest();
        _job.ShouldBeResumable = true;
        _job.CurrentState.Status = Status;
        _writesBefore = Writes;
        _changesBefore = _job.CurrentState.StatusChanges.Count;
    }
}
