// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

public class a_failed_parent_lookup : a_job_step_with_a_stored_parent
{
    protected Result<JobStepError> _stopped;

    protected async Task StopForRemoval()
    {
        _stopped = await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    protected void VerifyNoReport() => _parent.Verify(job => job.OnStepStopped(It.IsAny<JobStepId>(), It.IsAny<JobStepResult>()), Times.Never);
}
