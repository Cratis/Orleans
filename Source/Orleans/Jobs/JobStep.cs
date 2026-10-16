// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStep{TRequest, TResult, TState}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the step.</typeparam>
/// <typeparam name="TResult">Type of result for the step.</typeparam>
/// <typeparam name="TState">Type of state for the step.</typeparam>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="JobStep{TRequest, TResult, TState}"/> class.
/// </para>
/// <para>
/// Each start creates a run. The run's background work reaches its own activation only through
/// <see cref="TryInvokeOnActivation{T}"/>, never through calls to its own grain reference, so it keeps working
/// while the activation deactivates. Stopping and deactivating cancel the work and wait for it, within a deadline,
/// before the step records anything, and the run reports exactly one outcome to its job.
/// </para>
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="logger">The logger.</param>
[KeepAlive]
public abstract partial class JobStep<TRequest, TResult, TState>(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<TState> state,
    IJobStepThrottle throttle,
    ILogger<JobStep<TRequest, TResult, TState>> logger) : GrainWithBackgroundTask<TRequest, JobStepResult>, IJobStep<TRequest, TResult, TState>, IJobObserver, IDisposable
    where TState : JobStepState
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Work that outlived a timed-out stop may still release the gate; it holds no wait handle to free.")]
    readonly SemaphoreSlim _gate = new(1, 1);
    JobStepRun? _run;
    PendingStart? _pendingStart;
    IGrainTimer? _pendingStartTimer;
    long _lastRunId;
    bool _deactivated;
    IJob _job = new NullJob();
    JobStepCheckpointDebounce _checkpoints = new(1);
    TimeSpan _stopTimeout = new JobsOptions().StepStopTimeout;

    /// <summary>
    /// Gets the delivery of the current run's outcome, including a stop's removal of the step, which no stop,
    /// deactivation or start awaits.
    /// </summary>
    internal Task OutcomeDelivery => _run is { } run ? run.Stop ?? run.DeliverySettled : Task.CompletedTask;

    /// <summary>
    /// Gets the current run, if any.
    /// </summary>
    internal JobStepRun? CurrentRun => _run;

    /// <summary>
    /// Gets the <see cref="JobStepId"/> for this job step.
    /// </summary>
    protected JobStepId JobStepId => this.GetPrimaryKey();

    /// <summary>
    /// Gets a value indicating whether the step must make its progress checkpoint durable after every reported
    /// batch, overriding <see cref="JobsOptions.StepCheckpointBatchInterval"/>.
    /// </summary>
    /// <remarks>
    /// Debouncing is only safe to the extent that redelivering the batches it leaves unpersisted is harmless. A
    /// step whose subscriber cannot make redelivery a no-op overrides this to shrink its window to the same single
    /// batch the live delivery path already has. That is window equalization, not idempotency - a crash between
    /// handing a batch off and writing the checkpoint still re-delivers that batch.
    /// </remarks>
    protected virtual bool CheckpointAfterEveryBatch => false;

    /// <summary>
    /// Gets the parent job id.
    /// </summary>
    protected JobId JobId { get; private set; } = null!;

    /// <summary>
    /// Gets the state for the job step.
    /// </summary>
#pragma warning disable CA1721
    protected TState State => state.State;
#pragma warning restore CA1721

    /// <summary>
    /// Gets the <see cref="JobStepIdentifier"/>.
    /// </summary>
    protected JobStepIdentifier Identifier { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="JobStepName"/>.
    /// </summary>
    protected JobStepName Name { get; private set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        // Backstop: nothing that outlives the activation may still be admitted onto it.
        _deactivated = true;
        _run?.CloseAdmission();
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key!;
        var grainType = this.GetGrainType();
        Name = GetType().Name;
        JobId = jobStepKey.JobId;
        Identifier = new(jobStepKey.JobId, JobStepId);

        state.State.Name = Name;
        state.State.Id = Identifier;
        state.State.Type = grainType;

        var jobs = ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value;
        _checkpoints = new(jobs?.StepCheckpointBatchInterval ?? 1);
        _stopTimeout = jobs?.StepStopTimeout ?? _stopTimeout;

        var flushInterval = jobs?.StepCheckpointFlushInterval ?? TimeSpan.Zero;

        // A step that checkpoints after every batch never leaves one pending, so the timer would only ever find
        // nothing to do.
        if (_checkpoints.BatchInterval > 1 && flushInterval > TimeSpan.Zero && !CheckpointAfterEveryBatch)
        {
            this.RegisterGrainTimer(
                _ => UnderGate(FlushDebouncedCheckpoint),
                new GrainTimerCreationOptions
                {
                    DueTime = flushInterval,
                    Period = flushInterval
                });
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// Cancels in-flight work and waits for it to settle, bounded by the deactivation's cancellation token. Nothing
    /// is recorded or reported: the step stays recoverable.
    /// </summary>
    /// <param name="reason">The <see cref="DeactivationReason"/> for the deactivation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await DrainForDeactivation(cancellationToken);
        _deactivated = true;
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<StartJobStepError>> Start(GrainId jobGrainId)
    {
        using var scope = logger.BeginJobStepScope(State);

        // Read and removed before anything else, so the generation never flows into the work or its calls.
        var generation = JobExecutionGeneration.Take();

        // A job resumed while this step's stop of an earlier execution is still settling starts the step again once
        // that stop has reported - never alongside it, and never by reporting the old stop as the new run's.
        if (_run is { IsSettled: false, Phase: JobStepRunPhase.Stopping } stopping && IsLaterGeneration(generation, stopping.Generation))
        {
            logger.DeferringStartUntilStopSettles(generation!.Value);
            _pendingStart = new(jobGrainId, generation.Value);
            return Result.Success<StartJobStepError>();
        }

        return await StartRun(jobGrainId, generation);
    }

    /// <inheritdoc/>
    public Task<Result<PrepareJobStepError>> Prepare(object request) => Prepare(request, JobStepStage.First);

    /// <inheritdoc/>
    public async Task<Result<PrepareJobStepError>> Prepare(object request, JobStepStage stage)
    {
        using var scope = logger.BeginJobStepScope(State);
        var prepareTask = request switch
        {
            TRequest tRequest => Prepare(tRequest),
            _ => Task.FromResult<Result<PrepareJobStepError>>(PrepareJobStepError.WrongRequestType)
        };
        var prepareResult = await prepareTask;
        if (!prepareResult.IsSuccess)
        {
            return prepareResult;
        }

        State.IsPrepared = true;
        State.Stage = stage;
        _ = await UnderGate(WriteStateAsync);
        return prepareResult;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> ReportStatusChange(JobStepStatus status)
    {
        using var scope = logger.BeginJobStepScope(State);
        return await WriteStatusChangeUnderGate(null, status);
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> ReportFailure(PerformJobStepError error)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            return await ReportFailureToJob(null, error);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    public async Task<Result<PrepareJobStepError>> Prepare(TRequest request)
    {
        try
        {
            return await (await PrepareStep(request)).Match(
                async none =>
                {
                    await InitializeState(request);
                    return Result<PrepareJobStepError>.Success();
                },
                async error =>
                {
                    _ = await WriteStatusChangeUnderGate(null, JobStepStatus.Failed, [error.ToString()]);
                    return Result.Failed(error);
                });
        }
        catch (Exception ex)
        {
            logger.FailedPreparing(ex, Name);
            _ = await WriteStatusChangeUnderGate(null, JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
            return PrepareJobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public Task<TState> GetState() => Task.FromResult(State);

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task<Result<PrepareJobStepError>> PrepareStep(TRequest request);

    /// <summary>
    /// Resolves a grain reference to this step.
    /// </summary>
    /// <remarks>
    /// The library no longer uses this reference: since 1.19.0 a step's background work reaches its activation
    /// through <see cref="TryInvokeOnActivation{T}"/>. A call through this reference made while the activation
    /// deactivates is held and then delivered to a new activation, so step work should not use it either.
    /// </remarks>
    /// <returns>The grain reference to this step.</returns>
    protected virtual IJobStep<TRequest, TResult, TState> GetReferenceToSelf() => this.AsReference<IJobStep<TRequest, TResult, TState>>();

    /// <summary>
    /// Resolves the job grain this step reports its outcome to. Virtual for the same reason as
    /// <see cref="GetReferenceToSelf"/>.
    /// </summary>
    /// <param name="jobGrainId">The identity of the owning job grain.</param>
    /// <returns>The job grain.</returns>
    protected virtual IJob GetJob(GrainId jobGrainId) => GrainFactory.GetGrain<IJob>(jobGrainId);

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <remarks>
    /// This runs outside the activation, on a snapshot of the state. Reach the activation - to update
    /// <see cref="State"/> or write a checkpoint - through <see cref="TryInvokeOnActivation{T}"/>, never through a
    /// call to this grain's own reference: such a call is held while the activation deactivates and then reaches a
    /// new activation.
    /// </remarks>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Cancellation token that can cancel the step work.</param>
    /// <returns>True if successful, false if not.</returns>
    protected abstract Task<Catch<JobStepResult>> PerformStep(TState currentState, CancellationToken cancellationToken);

    /// <summary>
    /// Initialize the state from the initial request arguments.
    /// </summary>
    /// <remarks>This method is invoked after <see cref="PrepareStep"/> successfully completed.</remarks>
    /// <param name="request">The <typeparamref name="TRequest"/> initial request arguments.</param>
    /// <returns>The <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected abstract ValueTask InitializeState(TRequest request);

    /// <summary>
    /// Saves the current grain state.
    /// </summary>
    /// <returns>A task representing the asynchronous action.</returns>
    protected async Task<Catch> WriteStateAsync()
    {
        try
        {
            await state.WriteStateAsync();
            _checkpoints.Persisted();
            return Catch.Success();
        }
        catch (Exception ex)
        {
            logger.FailedToWriteState(ex);
            return ex;
        }
    }

    /// <summary>
    /// Saves the current grain state as a progress checkpoint, debounced so the write only happens once
    /// <see cref="JobsOptions.StepCheckpointBatchInterval"/> checkpoints have accumulated.
    /// </summary>
    /// <returns>A task representing the asynchronous action.</returns>
    /// <remarks>
    /// The in-memory state has already advanced when this is called; the write is deferred and any other state
    /// write (such as a terminal status change) flushes the pending checkpoint, as does the timed flush governed
    /// by <see cref="JobsOptions.StepCheckpointFlushInterval"/>. A step always resumes from its last
    /// persisted checkpoint and re-reads forward, so a crash between debounced writes re-delivers every batch
    /// handled since — an at-least-once relaxation rather than a free one. A consumer whose sink writes are
    /// naturally idempotent absorbs it; everything else opts out through <see cref="CheckpointAfterEveryBatch"/>.
    /// </remarks>
    protected Task<Catch> WriteCheckpointDebounced() =>
        _checkpoints.Report(CheckpointAfterEveryBatch)
            ? WriteStateAsync()
            : Task.FromResult(Catch.Success());

    /// <summary>
    /// Writes a progress checkpoint that debouncing has left unpersisted, doing nothing when there is none.
    /// </summary>
    /// <returns>A task representing the asynchronous action.</returns>
    /// <remarks>
    /// Driven by a timer on the <see cref="JobsOptions.StepCheckpointFlushInterval"/> cadence, because the
    /// batch counter alone bounds nothing in time: a step that hands off a few batches and then goes quiet would
    /// otherwise hold them unpersisted for as long as it stays activated.
    /// </remarks>
    protected Task<Catch> FlushDebouncedCheckpoint() =>
        _checkpoints.HasPendingCheckpoint ? WriteStateAsync() : Task.FromResult(Catch.Success());

    /// <summary>
    /// Create <typeparamref name="TResult"/> result when the job step is stopped without having the job step task running.
    /// </summary>
    /// <param name="currentState">The current state.</param>
    /// <remarks>
    /// If no kind of result can be derived from the current state null can be returned.
    /// </remarks>
    /// <returns>The result.</returns>
    protected abstract ValueTask<TResult?> CreateCancelledResultFromCurrentState(TState currentState);

    async Task<Result<StartJobStepError>> StartRun(GrainId jobGrainId, long? generation)
    {
        // Reserve before yielding: Start may interleave with another Start, a stop or an outcome delivery waiting
        // on the parent. A run whose work, callbacks or delivery have not settled is still live.
        if (_run is { IsSettled: false })
        {
            return StartJobStepError.AlreadyStarted;
        }
        if (!State.IsPrepared)
        {
            return StartJobStepError.NotPrepared;
        }
        switch (State.Status)
        {
            case JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure:
                return StartJobStepError.Completed;
            case JobStepStatus.Failed:
                return StartJobStepError.UnrecoverableFailedState;
        }

        var run = new JobStepRun(++_lastRunId, TaskScheduler.Current, generation);
        _run = run;
        _pendingStart = null;
        _job = GetJob(jobGrainId);
        try
        {
            return await DispatchPreparedWork(run);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return StartJobStepError.Unknown;
        }
        finally
        {
            run.Started();
        }
    }

    async Task<Result<StartJobStepError>> DispatchPreparedWork(JobStepRun run)
    {
        // Persist before dispatch so the worker cannot race its own reservation write.
        var previousStatus = State.Status;
        if (!(await WriteStatusChangeUnderGate(null, JobStepStatus.Scheduled)).IsSuccess)
        {
            return StartJobStepError.Unknown;
        }

        // A stop that began while Scheduled was written owns the outcome; nothing is launched for it to wait on.
        if (run.IsDraining)
        {
            return Result.Success<StartJobStepError>();
        }

        try
        {
            if (await Dispatch(run))
            {
                run.Running();
                return Result.Success<StartJobStepError>();
            }
        }
        catch (Exception)
        {
            _ = await WriteStatusChangeUnderGate(null, previousStatus);
            throw;
        }

        // A retained finished background task can refuse dispatch. Do not claim it is live or leave
        // a Scheduled reservation with no performer. The caller receives a startup failure.
        _ = await WriteStatusChangeUnderGate(null, previousStatus);
        return StartJobStepError.Unknown;
    }

    async Task<bool> Dispatch(JobStepRun run)
    {
        // The background task captures the run from this execution context; the work never reads _run.
        var previous = JobStepRun.Current;
        JobStepRun.Current = run;
        try
        {
            if (!await Start(CancellationToken.None))
            {
                return false;
            }
            run.WorkerSettled = WorkSettled;
            return true;
        }
        finally
        {
            JobStepRun.Current = previous;
        }
    }

    void TryDeactivateOnIdle()
    {
        // A deferred start runs on this activation; deactivating would lose it.
        if (_pendingStart is not null)
        {
            return;
        }

        try
        {
            DeactivateOnIdle();
        }
        catch (InvalidOperationException)
        {
            // Grain activation may already be invalid during silo shutdown.
        }
    }

    Task<Result<JobStepError>> WriteStatusChangeUnderGate(JobStepRun? run, JobStepStatus status, IEnumerable<string>? exceptionMessages = null, string? exceptionStackTrace = null) =>
        UnderGate(() =>
        {
            // A delivery outlives the turn that started it: never write for a run the activation no longer owns.
            if (run is not null && !IsCurrent(run))
            {
                logger.SkippedWriteForEndedRun(status);
                return Task.FromResult<Result<JobStepError>>(JobStepError.Unknown);
            }
            return WriteStatusChange(status, exceptionMessages, exceptionStackTrace);
        });

    async Task<T> UnderGate<T>(Func<Task<T>> operation)
    {
        // Only gated entry points come through here; code already holding the gate calls the core directly,
        // because the gate is not reentrant.
        await _gate.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            _gate.Release();
        }
    }

    bool IsCurrent(JobStepRun run) => !_deactivated && ReferenceEquals(run, _run);

    async Task<Result<JobStepError>> WriteStatusChange(JobStepStatus status, IEnumerable<string>? exceptionMessages = null, string? exceptionStackTrace = null)
    {
        try
        {
            logger.ChangingStatus(status);
            StatusChanged(status, exceptionMessages, exceptionStackTrace);
            var writeResult = await WriteStateAsync();
            return writeResult.Match(
                _ => Result<JobStepError>.Success(),
                ex => HandleFailedToWriteStatusChange(ex));
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }

        JobStepError HandleFailedToWriteStatusChange(Exception ex)
        {
            logger.FailedToWriteState(ex, status);
            return JobStepError.FailedToPersistState;
        }
    }

    void StatusChanged(JobStepStatus status, IEnumerable<string>? exceptionMessages = null, string? exceptionStackTrace = null)
    {
        state.State.StatusChanges.Add(new()
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionMessages = exceptionMessages ?? [],
            ExceptionStackTrace = exceptionStackTrace ?? string.Empty
        });
        state.State.Status = status;
    }
}
