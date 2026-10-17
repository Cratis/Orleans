// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs;

public abstract partial class JobStep<TRequest, TResult, TState>
{
    enum ParentResolution
    {
        Resolved = 0,
        Missing = 1,
        Failed = 2
    }

    async Task<bool> PrepareStopDelivery(JobStepRun run, CancellationToken deadline)
    {
        var resolution = await ResolveParent(run, deadline);
        if (resolution is ParentResolution.Resolved)
        {
            return true;
        }
        if (resolution is ParentResolution.Missing)
        {
            if (deadline.IsCancellationRequested)
            {
                logger.ParentLookupTimedOut(JobId);
            }
            else if (run.Phase is JobStepRunPhase.Removing)
            {
                logger.RemovingStepWithoutParentJob(JobStepId, JobId);
                if ((await WriteStatusChangeUnderGate(run, JobStepStatus.Removing)).TryGetError(out var error))
                {
                    run.StopError = error;

                    // Removing is already in memory, but the delete may not have reached storage. A fresh
                    // activation must reload durable state before retrying; nothing queued here may trust it.
                    _pendingStart = null;
                    _deactivated = true;
                    run.CloseAdmission();
                }
                TryDeactivateOnIdle();
                return false;
            }
            else
            {
                logger.ParentJobNotFound(JobId);
            }
        }

        run.StopError = JobStepError.FailedToReportToJob;
        return false;
    }

    async Task<ParentResolution> ResolveParent(JobStepRun run, CancellationToken deadline)
    {
        if (_job is not null)
        {
            return ParentResolution.Resolved;
        }

        try
        {
            deadline.ThrowIfCancellationRequested();
            _ = this.GetPrimaryKey(out var extension);
            var key = (JobStepKey)extension!;
            var storage = ServiceProvider.GetRequiredService<IJobsStorage>().GetFor(key.Scope, key.Namespace);

            // Dispatch through the provider interface: metadata-only overrides stay in use, and third-party
            // providers without one retain IJobStorage's full-job compatibility fallback.
            var storedType = await storage.Jobs.GetJobType(JobId).WaitAsync(deadline);
            deadline.ThrowIfCancellationRequested();
            if (storedType.TryGetError(out var error))
            {
                if (error is Storage.Jobs.JobError.NotFound)
                {
                    return ParentResolution.Missing;
                }

                logger.ParentJobReadFailed(JobId, error);
                return ParentResolution.Failed;
            }
            if (storedType.TryGetException(out var exception))
            {
                logger.ParentLookupFailed(JobId, exception);
                return ParentResolution.Failed;
            }

            var type = storedType.AsT0;
            var mapped = ServiceProvider.GetRequiredService<IJobTypes>().GetClrTypeFor(type);
            if (!mapped.TryGetResult(out var clrType))
            {
                logger.ParentJobTypeUnavailable(JobId, type);
                return ParentResolution.Failed;
            }

            deadline.ThrowIfCancellationRequested();
            if (!IsCurrent(run))
            {
                logger.ParentLookupActivationEnded(JobId);
                return ParentResolution.Failed;
            }

            var parent = GrainFactory.GetGrain(clrType, JobId, new JobKey(key.Scope, key.Namespace));
            _job = GetJob(parent.GetGrainId());
            return ParentResolution.Resolved;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            logger.ParentLookupTimedOut(JobId);
            return ParentResolution.Failed;
        }
        catch (Exception exception)
        {
            logger.ParentLookupFailed(JobId, exception);
            return ParentResolution.Failed;
        }
    }
}
