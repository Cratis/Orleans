// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager;

public class when_deactivation_is_requested_during_rehydration : given.a_jobs_manager_with_lifecycle
{
    bool _onDeactivateWasDeferred;
    bool _hostWasStillRunning;
    bool _firstReadWasStillPending;

    async Task Establish()
    {
        await _manager.Rehydrate().WaitAsync(Deadline);
        await _lifecycle.FirstReadStarted.Task.WaitAsync(Deadline);
    }

    async Task Because()
    {
        var deactivation = _manager.DeactivateWhileBusy();
        await _lifecycle.DeactivationRequested.Task.WaitAsync(Deadline);
        await _lifecycle.DrainStopped.Task.WaitAsync(Deadline);
        _firstReadWasStillPending = !_lifecycle.AllowFirstRead.Task.IsCompleted;
        _onDeactivateWasDeferred = !_lifecycle.OnDeactivateCalled.Task.IsCompleted;
        _hostWasStillRunning = !((InProcessSiloHandle)_cluster.Primary).SiloHost.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested;
        _lifecycle.AllowFirstRead.SetResult();
        _lifecycle.FinishRequest.SetResult();
        await deactivation.WaitAsync(Deadline);
        await _lifecycle.OnDeactivateCalled.Task.WaitAsync(Deadline);
    }

    [Fact] void should_stop_without_waiting_for_an_in_flight_storage_read() => _firstReadWasStillPending.ShouldBeTrue();
    [Fact] void should_stop_without_waiting_for_the_on_deactivate_hook() => _onDeactivateWasDeferred.ShouldBeTrue();
    [Fact] void should_stop_while_the_host_is_still_running() => _hostWasStillRunning.ShouldBeTrue();
    [Fact] void should_not_admit_another_job_after_deactivation_is_requested() => _lifecycle.JobStorage.Received(1).GetJob(Arg.Any<JobId>());
}
