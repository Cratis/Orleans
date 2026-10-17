// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// The owner stops admitting the job, and then the silo hosting it dies. Rehydration finds the job running, is
/// refused, and the job ends up stopped without its step ever being performed again - through every way of resuming
/// it, and after the silo hosting it restarts.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class when_admission_refuses_a_job_whose_silo_died : Specification
{
    RecoveryClusterFixture _fixture;
    JobId _jobId;
    JobState _stopped;
    bool _managerResumed;
    Result<ResumeJobSuccess, ResumeJobError> _jobResume;
    JobState _afterRestart;
    int _performersAfterRestart;

    void Establish() => _fixture = new();

    async Task Because()
    {
        var (id, job) = await _fixture.Start();
        _jobId = id;
        _fixture.Signals.RefuseAdmission = true;
        await _fixture.Kill();

        // Membership-triggered rehydration is refused; the job stops and its drain settles on its own turn.
        await _fixture.Signals.AdmissionRefusals.WaitAsync(RecoveryClusterFixture.Deadline);
        await _fixture.Signals.Stopped.Task.WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _stopped = await job.Inspect();
        _managerResumed = await _fixture.Manager.Resume(id).WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        _jobResume = await job.Resume();

        var host = await job.Silo();
        var hostHandle = _fixture.Cluster.Silos.Single(silo => ((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress.Equals(host));
        while (_fixture.Signals.RecoveryPasses.Wait(0))
        {
        }
        await _fixture.Cluster.RestartSiloAsync(hostHandle).WaitAsync(RecoveryClusterFixture.Deadline, TimeProvider.System);
        await _fixture.Signals.RecoveryPasses.WaitAsync(RecoveryClusterFixture.Deadline);
        _afterRestart = await job.Inspect();
        _performersAfterRestart = _fixture.Signals.Performers.Count;
    }

    [Fact] void should_leave_the_job_stopped() => _stopped.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_have_settled_the_drain() => _stopped.DrainPending.ShouldBeFalse();
    [Fact] void should_not_perform_the_step_again() => _performersAfterRestart.ShouldEqual(1);
    [Fact] void should_not_resume_through_the_manager() => _managerResumed.ShouldBeFalse();
    [Fact] void should_report_not_admitted_when_resumed_directly() => ((CannotResumeJobError)_jobResume.AsT1).ShouldEqual(CannotResumeJobError.NotAdmitted);
    [Fact] void should_still_be_stopped_after_the_silo_restarts() => _afterRestart.Status.ShouldEqual(JobStatus.Stopped);

    void Destroy() => _fixture.Dispose();
}
