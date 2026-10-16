// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// A step that performs short real work for one item.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IntegrationJobStep"/> class.
/// </remarks>
/// <param name="state">The persisted state of the step.</param>
/// <param name="throttle">The throttle limiting parallel steps.</param>
/// <param name="logger">The logger.</param>
public class IntegrationJobStep(
    [PersistentState(nameof(IntegrationJobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<IntegrationJobStepState> state,
    IJobStepThrottle throttle,
    ILogger<IntegrationJobStep> logger) : JobStep<string, string, IntegrationJobStepState>(state, throttle, logger), IIntegrationJobStep
{
    /// <summary>
    /// A step whose item contains this fails, so a spec can drive the failure path without a second step type.
    /// </summary>
    public const string FailingItem = "fail";

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(string request) =>
        Task.FromResult(Result<PrepareJobStepError>.Success());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(string request)
    {
        State.Item = request;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        IntegrationJobStopSignals.Find(JobId)?.StepDeactivating.TrySetResult();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        var signals = IntegrationJobStopSignals.Find(JobId);
        if (signals is not null)
        {
            signals.WorkEntered.TrySetResult();
            await signals.ContinueWork.Task;
        }
        var result = await base.PerformWork();
        signals?.WorkCompleted.TrySetResult(result);
        return result;
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(IntegrationJobStepState currentState, CancellationToken cancellationToken)
    {
        IntegrationJobStopSignals.Find(JobId)?.Performed.TrySetResult();
        var started = DateTimeOffset.UtcNow;
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        IntegrationJobStepTimeline.Record(new(currentState.Item, started, DateTimeOffset.UtcNow));
        if (string.Equals(currentState.Item, FailingItem, StringComparison.Ordinal))
        {
            return Catch<JobStepResult>.Success(JobStepResult.Failed($"'{currentState.Item}' was asked to fail"));
        }

        return Catch<JobStepResult>.Success(JobStepResult.Succeeded(currentState.Item));
    }

    /// <inheritdoc/>
    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(IntegrationJobStepState currentState) =>
        ValueTask.FromResult<string?>(string.Empty);
}
