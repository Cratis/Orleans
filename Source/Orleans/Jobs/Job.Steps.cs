// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    int MaxConcurrentJobStepStartups =>
        Math.Max(1, ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value?.MaxConcurrentStepStarts ?? new JobsOptions().MaxConcurrentStepStarts);

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var (ignored, attempt) = await IgnoreStepOutcome(stepId, stopped: false);
        if (ignored)
        {
            return Result.Success<JobError>();
        }
        _logger.StepSuccessfullyCompleted(stepId);
        OutcomeCounted(stepId);
        _acceptedOutcomes.Add((stepId, attempt));
        _attemptsToRun.Remove(stepId);
        State.Progress.SuccessfulSteps++;
        CountStepOutcomeInStage(stepId, succeeded: true);
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        if ((await IgnoreStepOutcome(stepId, stopped: true)).Ignored)
        {
            return Result.Success<JobError>();
        }
        _logger.StepStopped(stepId);
        State.Progress.StoppedSteps++;
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var (ignored, attempt) = await IgnoreStepOutcome(stepId, stopped: false);
        if (ignored)
        {
            return Result.Success<JobError>();
        }

        _acceptedOutcomes.Add((stepId, attempt));
        if (DecidesToRetry(stepId, jobStepResult, attempt))
        {
            // The failed attempt is persisted by the step; the job runs the next one after its backoff and counts
            // nothing yet.
            ScheduleStepRetry(stepId, attempt);
            return Result.Success<JobError>();
        }

        _logger.StepFailed(stepId);
        OutcomeCounted(stepId);
        _attemptsToRun.Remove(stepId);
        State.Progress.FailedSteps++;
        CountStepOutcomeInStage(stepId, succeeded: false);
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <summary>
    /// Called when a step has completed successfully, completed with failure or stopped.
    /// </summary>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <param name="result"><see cref="JobStepResult"/> for the step.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result) => Task.CompletedTask;

    /// <summary>
    /// Create a new <see cref="IJobStep"/>.
    /// </summary>
    /// <param name="request">The request associated with the step.</param>
    /// <typeparam name="TJobStep">The type of job step to create.</typeparam>
    /// <returns>A new instance of the job step.</returns>
    protected JobStepDetails CreateStep<TJobStep>(object request)
        where TJobStep : IJobStep
    {
        var jobStepId = JobStepId.New();
        var jobId = this.GetPrimaryKey(out var keyExtension);
        var jobKey = (JobKey)keyExtension!;
        var jobStepType = typeof(TJobStep)
            .AllBaseAndImplementingTypes()
            .First(
                _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IJobStep<,,>));
        var resultType = jobStepType.GetGenericArguments()[1];
        return new(
            typeof(TJobStep),
            jobStepId,
            new(jobId, jobKey.Scope, jobKey.Namespace),
            request,
            resultType);
    }

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request associated with the job.</param>
    /// <returns>Collection of <see cref="JobStepDetails"/> .</returns>
    protected abstract Task<IImmutableList<JobStepDetails>> PrepareSteps(TRequest request);

    async Task<Result<JobError>> PerformStepEventHandling(JobStepId stepId, JobStepResult jobStepResult)
    {
        _hasLiveStepTracking = true;
        try
        {
            _ = await WriteState();
            var handleCompletedStepResult = await HandleJobStepCompletedOrStopped(stepId, jobStepResult);
            return handleCompletedStepResult.Match(
                _ => Result.Success<JobError>(),
                error =>
                {
                    _logger.FailedHandlingCompletedJobStep(stepId, error);
                    return Result.Failed(error);
                });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Result<JobError>> HandleJobStepCompletedOrStopped(JobStepId stepId, JobStepResult result)
    {
        try
        {
            await OnStepCompletedOrStopped(stepId, result);

            // While a drain is pending only the drain settles a stopped job; a report merely counts its step. A removal
            // takes the drain over: it completes once every step has reported, and clearing the job ends the drain.
            if (State.DrainPending && State.Status is JobStatus.Stopped)
            {
                return Result.Success<JobError>();
            }
            await AdvanceStagesAfter(stepId);
            var handleCompletionResult = await HandleCompletion();
            return await HandleCompletionResult(handleCompletionResult);
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
        finally
        {
            try
            {
                if (_jobStepGrains?.ContainsKey(stepId) != true)
                {
                    _logger.CompletedJobStepIsNoLongerTracked(stepId);
                }

                // Subscription identity survives a lost tracking entry; no scan of the other steps is needed.
                await UnsubscribeJobStep(stepId);
            }
            finally
            {
                // An unsubscribe failure must not hand an accounted-for step back to recovery.
                _jobStepGrains?.Remove(stepId);
                LeaveWindow(stepId);
            }

            // A step that reached its outcome leaves room for the next one.
            await FillWindow();
        }
    }

    async Task<Result<JobError>> HandleCompletionResult(Result<HandleCompletionSuccess, JobError> completionResult)
    {
        if (completionResult.TryGetError(out var handleCompletionError))
        {
            return handleCompletionError;
        }
        var needsToWriteState = completionResult.AsT0 switch
        {
            HandleCompletionSuccess.ClearedState or HandleCompletionSuccess.NotClearedState => false,
            _ => true
        };
        if (!needsToWriteState)
        {
            return Result<JobError>.Success();
        }
        var writeStateResult = await WriteState();
        return writeStateResult.Match(
            _ => Result<JobError>.Success(),
            ex =>
            {
                _logger.FailedUpdatingStateAfterHandlingCompletion(ex);
                return JobError.PersistStateError;
            });
    }

    ReadOnlyDictionary<JobStepId, JobStepGrainAndRequest> CreateGrainsFromJobSteps(IImmutableList<JobStepDetails> jobSteps) =>
        jobSteps.ToDictionary(
            details => details.Id,
            details => new JobStepGrainAndRequest(GetJobStepGrain(details), details.Request)).AsReadOnly();

    async Task<Dictionary<JobStepId, IJobStep>> GetIdAndGrainReferenceJobSteps(params JobStepStatus[] statuses)
    {
        var getJobSteps = await Storage.JobSteps.GetForJob(JobId, statuses);
        getJobSteps.RethrowError();
        var jobSteps = getJobSteps.AsT0;
        TrackStagesOf(jobSteps);
        foreach (var jobStep in jobSteps.Where(_ => _.Status is JobStepStatus.Scheduled or JobStepStatus.Running))
        {
            // Steps that were running when the job was left stay in the window whatever its size.
            _inFlight.Add(jobStep.Id.JobStepId);
        }
        return jobSteps.ToDictionary(jobStep => jobStep.Id.JobStepId, GetJobStepGrain);
    }
    Task<Dictionary<JobStepId, IJobStep>> GetIdAndGrainReferenceForNonCompletedJobSteps() =>
        GetIdAndGrainReferenceJobSteps(JobStepStatus.Running, JobStepStatus.Scheduled, JobStepStatus.Unknown, JobStepStatus.Stopped);

    /// <summary>
    /// Recount the job's progress from the persisted state of its steps.
    /// </summary>
    /// <param name="countStopped">Whether stopped steps count as stopped, as they do when a stop settles, rather than as left to resume.</param>
    /// <returns>The steps the progress was recounted from, or null when they could not be read.</returns>
    /// <remarks>
    /// The progress is maintained by counting step outcomes as they are reported, so an outcome that never reached
    /// storage leaves it permanently short of the total and the job never finishes (#3944). The steps are the record
    /// of what actually happened, so they are what a stalled job's progress is rebuilt from.
    /// </remarks>
    async Task<IImmutableList<JobStepState>?> ReconcileProgressFromJobSteps(bool countStopped = false)
    {
        var getJobSteps = await Storage.JobSteps.GetForJob(JobId);
        if (getJobSteps.TryGetException(out var exception))
        {
            _logger.FailedReconcilingProgressFromJobSteps(exception);
            return null;
        }

        var steps = getJobSteps.AsT0;
        State.Progress.TotalSteps = steps.Count;
        State.Progress.SuccessfulSteps = steps.Count(_ => _.Status is JobStepStatus.CompletedSuccessfully);
        State.Progress.FailedSteps = steps.Count(_ => _.Status is JobStepStatus.CompletedWithFailure or JobStepStatus.Failed);
        State.Progress.UnreachableSteps = steps.Count(_ => _.Status is JobStepStatus.Unreachable);
        State.Progress.StoppedSteps = countStopped ? steps.Count(_ => _.Status is JobStepStatus.Stopped) : 0;
        State.Progress.Stages = JobStages.Reconcile(steps, State.Progress.Stages);
        TrackStagesOf(steps);
        return steps;
    }

    IJobStep GetJobStepGrain(JobStepDetails details) => (GrainFactory.GetGrain(details.Type, details.Id, keyExtension: details.Key) as IJobStep)!;
    IJobStep GetJobStepGrain(JobStepState state) => (GrainFactory.GetGrain((Type)state.Type, state.Id.JobStepId, keyExtension: new JobStepKey(state.Id.JobId, JobKey.Scope, JobKey.Namespace)) as IJobStep)!;

    async Task<Result<StartJobError>> PrepareAndStartRunningAllSteps(TRequest request)
    {
        try
        {
            var grainId = this.GetGrainId();
            var steps = await PrepareSteps(request);
            PlanStages(steps);
            State.StepPlan = SaveStepPlan(steps);
            State.Progress.TotalSteps = steps.Count;
            if (!await TryWriteStatusChanged(JobStatus.PreparingSteps))
            {
                DeactivateOnIdleDuringShutdown();
                return StartJobError.Unknown;
            }
            if (steps.Count == 0)
            {
                _logger.NoJobStepsToStart();
                _ = HandleCompletionResult(await HandleCompletion());
                return StartJobError.NoJobStepsToStart;
            }

            _logger.PreparingJobSteps(steps.Count);
            return await PrepareAndStartAllJobSteps(grainId, steps);
        }
        catch (Exception ex)
        {
            _logger.ErrorPreparingJobSteps(ex);
            _ = await WriteStatusChanged(JobStatus.Failed, ex);
            return ex is JobStepRequestCannotRoundTrip ? StartJobError.InvalidStepPlan : StartJobError.Unknown;
        }
    }

    async Task<Result<StartJobError>> PrepareAndStartAllJobSteps(GrainId grainId, IImmutableList<JobStepDetails> jobSteps)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var jobStepGrainAndRequests = CreateGrainsFromJobSteps(jobSteps);
        _jobStepGrains = jobStepGrainAndRequests.ToDictionary(_ => _.Key, _ => _.Value.Grain);
        var prepareAllJobStepsResult = await TryPrepareAllJobSteps(jobStepGrainAndRequests);

        if (prepareAllJobStepsResult.TryGetResult(out var preparedAllJobSteps) && !preparedAllJobSteps)
        {
#pragma warning disable CA2201
            _ = await WriteStatusChanged(JobStatus.Failed, new("Failed while preparing job steps. Could not prepare all steps."));
#pragma warning restore CA2201
            return StartJobError.CouldNotPrepareJobSteps;
        }
        if (!await TryWriteStatusChanged(JobStatus.StartingSteps))
        {
            DeactivateOnIdleDuringShutdown();
            return StartJobError.Unknown;
        }

        // For a job with stages only the first one starts here - the rest follow as each stage completes.
        await OnBeforeStartingJobSteps();
        await RunDispatchHook(initializationMayBeIncomplete: false);
        var startJobStepsResult = await StartFirstStage(grainId);

        if (startJobStepsResult.TryGetError(out var startJobStepsError))
        {
            if (startJobStepsError is not (StartJobError.AllJobStepsFailedStarting or StartJobError.FailedStartingSomeJobSteps))
            {
                return startJobStepsError;
            }

            _ = await HandleCompletionResult(await HandleCompletion());
            if (State.Status is JobStatus.StartingSteps && !await TryEnterRunning())
            {
                return StartJobError.Unknown;
            }
            return startJobStepsError;
        }

        return await TryEnterRunning() ? Result<StartJobError>.Success() : StartJobError.Unknown;
    }

    async Task<Result<bool, OneOf.Types.None>> TryPrepareAllJobSteps(ReadOnlyDictionary<JobStepId, JobStepGrainAndRequest> jobStepRequests)
    {
        foreach (var chunk in jobStepRequests.Chunk(MaxConcurrentJobStepStartups))
        {
            var prepareResults = await Task.WhenAll(chunk.Select(async idAndGrainAndRequest =>
            {
                var stage = _jobStepStages.GetValueOrDefault(idAndGrainAndRequest.Key, JobStepStage.First);
                var prepareResult = await idAndGrainAndRequest.Value.Grain.Prepare(idAndGrainAndRequest.Value.Request, stage);
                return (idAndGrainAndRequest.Key, prepareResult);
            }));

            var allPreparedInChunk = true;
            foreach (var (id, prepareResult) in prepareResults)
            {
                if (!prepareResult.TryGetError(out var prepareJobStepError))
                {
                    continue;
                }
                _logger.FailedPreparingJobStep(id, prepareJobStepError);
                allPreparedInChunk = false;
            }

            if (!allPreparedInChunk)
            {
                return false;
            }
        }
        return true;
    }

    async Task<Result<StartJobError>> StartAndSubscribeToJobSteps(GrainId grainId, Dictionary<JobStepId, IJobStep> allJobStepGrains)
    {
        await EnsureDispatchHook();
        _hasDispatchedSteps = true;
        _hasLiveStepTracking = true;
        var jobStepGrains = SelectWindow(allJobStepGrains);
        var stepsLeftWaiting = allJobStepGrains.Count - jobStepGrains.Count;
        var totalJobSteps = jobStepGrains.Count;
        var failedJobStepIds = new List<JobStepId>();
        foreach (var chunk in jobStepGrains.Chunk(MaxConcurrentJobStepStartups))
        {
            (JobStepId Id, Result<StartJobStepError> Result, Exception? Exception)[] startResults;
            using (JobExecutionGeneration.Carry(ExecutionGeneration()))
            {
                startResults = await Task.WhenAll(chunk.Select(idAndGrain => TryStartJobStep(idAndGrain.Key, idAndGrain.Value, grainId)));
            }

            foreach (var (id, startResult, exception) in startResults)
            {
                if (exception is not null)
                {
                    failedJobStepIds.Add(id);
                    _logger.FailedStartingJobStep(exception, id);
                    continue;
                }
                if (!startResult.TryGetError(out var startError))
                {
                    await SubscribeJobStep(jobStepGrains[id].AsReference<IJobObserver>());
                    continue;
                }
                if (startError is StartJobStepError.AlreadyStarted or StartJobStepError.Completed)
                {
                    _logger.JobStepWasAlreadyStarted(id, startError);
                    continue;
                }
                failedJobStepIds.Add(id);
                _logger.FailedStartingJobStep(id, startError);
            }
        }
        if (failedJobStepIds.Count == 0)
        {
            return Result<StartJobError>.Success();
        }
        await RecordJobStepsAsFailed(failedJobStepIds, jobStepGrains);
        State.Progress.FailedSteps += failedJobStepIds.Count;
        foreach (var failedJobStepId in failedJobStepIds)
        {
            CountStepOutcomeInStage(failedJobStepId, succeeded: false);
            if (EffectiveMaxActiveSteps is not null)
            {
                // A step that never started leaves its place in the window to the next one.
                LeaveWindow(failedJobStepId);
                _jobStepGrains?.Remove(failedJobStepId);
            }
        }
        return failedJobStepIds.Count == totalJobSteps && stepsLeftWaiting == 0
            ? StartJobError.AllJobStepsFailedStarting
            : StartJobError.FailedStartingSomeJobSteps;
    }

    async Task RecordJobStepsAsFailed(List<JobStepId> jobStepIds, Dictionary<JobStepId, IJobStep> jobStepGrains)
    {
        foreach (var chunk in jobStepIds.Chunk(MaxConcurrentJobStepStartups))
        {
            await Task.WhenAll(chunk.Select(id => RecordJobStepStatus(id, jobStepGrains[id], JobStepStatus.Failed)));
        }
    }

    async Task RecordJobStepStatus(JobStepId id, IJobStep jobStep, JobStepStatus status)
    {
        try
        {
            var reportResult = await jobStep.ReportStatusChange(status);
            if (reportResult.TryGetError(out var error))
            {
                _logger.FailedRecordingJobStepStatus(id, status, error);
            }
        }
        catch (Exception ex)
        {
            _logger.FailedRecordingJobStepStatus(ex, id, status);
        }
    }

    async Task<(JobStepId Id, Result<StartJobStepError> Result, Exception? Exception)> TryStartJobStep(JobStepId id, IJobStep jobStep, GrainId grainId)
    {
        try
        {
            return (id, _attemptsToRun.TryGetValue(id, out var attempt) ? await jobStep.Retry(grainId, attempt) : await jobStep.Start(grainId), null);
        }
        catch (Exception ex)
        {
            return (id, StartJobStepError.Unknown, ex);
        }
    }

    Task SubscribeJobStep(IJobObserver observer)
    {
        _observers?.Subscribe(observer.GetPrimaryKey(), observer);
        return Task.CompletedTask;
    }
    Task UnsubscribeJobStep(JobStepId stepId)
    {
        _observers?.Unsubscribe(stepId);
        ForgetStepNotification(stepId);
        return Task.CompletedTask;
    }
}
