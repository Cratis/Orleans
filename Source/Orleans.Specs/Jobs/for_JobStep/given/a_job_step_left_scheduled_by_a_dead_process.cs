// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Serialization;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// A job step whose persisted state says it is already Scheduled - exactly what a step looks like
/// when the process died between its work being scheduled and the work completing. The activation is
/// fresh, so no background task exists no matter what the persisted status claims.
/// </summary>
public class a_job_step_left_scheduled_by_a_dead_process : Specification
{
    protected TestKitSilo _silo = new();
    protected SomeJobStepGrain _jobStep;
    protected JobId _jobId;
    protected JobStepId _jobStepId;
    protected JobStepState _persistedState;
    protected StepStorage _storage;

    async Task Establish()
    {
        _jobId = Guid.Parse("fefd1ea0-f739-4d68-8817-6c85f722dec4");
        _jobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-000000000002");

        _persistedState = new JobStepState
        {
            Id = new(_jobId, _jobStepId),
            IsPrepared = true,
            Status = JobStepStatus.Scheduled
        };

        _storage = new(_persistedState);
        _silo.AddPersistentStateStorage(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps, _storage);
        _silo.AddService(Options.Create(new JobsOptions { StepCheckpointBatchInterval = 1, StepStopTimeout = StopTimeout }));
        _silo.AddService(CreateThrottle());
        if (RegisterStateCopier)
        {
            _silo.AddService(new ServiceCollection().AddCratisOrleansSerializers().BuildServiceProvider().GetRequiredService<DeepCopier<JobStepState>>());
        }
        _silo.AddService<ILogger<SomeJobStepGrain>>(NullLogger<SomeJobStepGrain>.Instance);

        var key = new JobStepKey(_jobId, "event-store", "namespace");
        _jobStep = await _silo.CreateGrainAsync<SomeJobStepGrain>(_jobStepId, keyExtension: key);
    }

    /// <summary>
    /// Gets a value indicating whether the silo can copy the step's state, as every real silo can.
    /// </summary>
    protected virtual bool RegisterStateCopier => true;

    /// <summary>
    /// Gets how long stopping the step waits for its work to settle.
    /// </summary>
    protected virtual TimeSpan StopTimeout => TimeSpan.FromSeconds(10);

    /// <summary>
    /// Creates the throttle used by the step in this specification.
    /// </summary>
    /// <returns>The throttle.</returns>
    protected virtual IJobStepThrottle CreateThrottle() => new PassThroughThrottle();

    sealed class PassThroughThrottle : IJobStepThrottle
    {
        public Task AcquireAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Release()
        {
        }
    }
}
