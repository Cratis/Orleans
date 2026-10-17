// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Recovers interrupted jobs after startup and cluster member failure, without reminders.
/// </summary>
/// <param name="membership">The authoritative cluster membership stream.</param>
/// <param name="localSilo">The current silo identity.</param>
/// <param name="keys">The host's manager key providers.</param>
/// <param name="grainFactory">Factory for resolving managers.</param>
/// <param name="logger">The logger.</param>
public sealed class JobsRecovery(
    IClusterMembershipService membership,
    ILocalSiloDetails localSilo,
    IInstancesOf<IJobsManagerKeys> keys,
    IGrainFactory grainFactory,
    ILogger<JobsRecovery> logger) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly CancellationTokenSource _stopping = new();
    readonly Channel<RecoveryFailures> _failures = Channel.CreateUnbounded<RecoveryFailures>(new UnboundedChannelOptions { SingleReader = true });
    readonly TimeProvider _timeProvider = TimeProvider.System;
    Task? _recovery;
    Task? _retry;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsRecovery"/> class with a controlled clock.
    /// </summary>
    /// <param name="membership">The authoritative cluster membership stream.</param>
    /// <param name="localSilo">The current silo identity.</param>
    /// <param name="keys">The host's manager key providers.</param>
    /// <param name="grainFactory">Factory for resolving managers.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The clock used for retry delays.</param>
    internal JobsRecovery(
        IClusterMembershipService membership,
        ILocalSiloDetails localSilo,
        IInstancesOf<IJobsManagerKeys> keys,
        IGrainFactory grainFactory,
        ILogger<JobsRecovery> logger,
        TimeProvider timeProvider)
        : this(membership, localSilo, keys, grainFactory, logger)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(JobsRecovery), ServiceLifecycleStage.Active + 1, Start, Stop);

    /// <inheritdoc/>
    public void Dispose() => _stopping.Dispose();

    Task Start(CancellationToken cancellationToken)
    {
        // Grain calls are made only after Active, off the lifecycle turn. A backlog never blocks startup.
        _retry = Task.Run(() => RunUntilStopped(Retry), CancellationToken.None);
        _recovery = Task.Run(() => RunUntilStopped(RecoverAndWatch), CancellationToken.None);
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync();
        await Task.WhenAll(_recovery ?? Task.CompletedTask, _retry ?? Task.CompletedTask).WaitAsync(cancellationToken);
    }

    async Task RunUntilStopped(Func<CancellationToken, Task> work)
    {
        try
        {
            await work(_stopping.Token);
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            // Shutdown cancels discovery and grain-call waits; durable jobs remain available for recovery.
        }
        catch (Exception exception)
        {
            logger.RecoveryFailed(exception);
        }
    }

    async Task RecoverAndWatch(CancellationToken cancellationToken)
    {
        var hasHostProvider = await Recover(cancellationToken);
        var dead = new HashSet<SiloAddress>();
        await foreach (var snapshot in membership.MembershipUpdates.WithCancellation(cancellationToken))
        {
            var newlyDead = snapshot.Members.Values
                .Where(member => member.Status is SiloStatus.Dead)
                .Select(member => member.SiloAddress)
                .Count(dead.Add);

            // Seen keys are silo-local: electing one survivor would lose the other survivors' catalogs.
            if (newlyDead > 0 && (!hasHostProvider || IsElected(snapshot)))
            {
                logger.RecoveringAfterMemberFailure(newlyDead);
                hasHostProvider = await Recover(cancellationToken);
            }
        }
    }

    async Task Retry(CancellationToken cancellationToken)
    {
        await foreach (var failures in _failures.Reader.ReadAllAsync(cancellationToken))
        {
            var pending = failures;
            var delay = TimeSpan.FromSeconds(1);
            var attempt = 0;
            while (pending.HasFailures)
            {
                MergeQueuedFailures(pending);
                logger.RetryingRecovery(pending.Providers.Count, pending.Keys.Count, ++attempt, delay);
                await Task.Delay(delay, _timeProvider, cancellationToken);
                pending = await Rehydrate(pending.Providers, pending.Keys, cancellationToken);
                MergeQueuedFailures(pending);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
            logger.RecoveryRetriesResolved();
        }
    }

    void MergeQueuedFailures(RecoveryFailures pending)
    {
        while (_failures.Reader.TryRead(out var failures))
        {
            pending.Providers.UnionWith(failures.Providers);
            pending.Keys.UnionWith(failures.Keys);
        }
    }

    bool IsElected(ClusterMembershipSnapshot snapshot) => snapshot.Members.Values
        .Where(member => member.Status is SiloStatus.Active)
        .OrderBy(member => member.SiloAddress.Generation)
        .ThenBy(member => member.SiloAddress.ToParsableString(), StringComparer.Ordinal)
        .FirstOrDefault()?.SiloAddress.Equals(localSilo.SiloAddress) == true;

    async Task<bool> Recover(CancellationToken cancellationToken)
    {
        var providers = keys.ToArray();
        var hasHostProvider = providers.Any(provider => provider is not JobsManagerKeys);
        if (!hasHostProvider)
        {
            logger.IncompleteRecoveryKeys();
        }
        var failures = await Rehydrate(providers, [], cancellationToken);
        if (failures.HasFailures)
        {
            await _failures.Writer.WriteAsync(failures, cancellationToken);
        }
        return hasHostProvider;
    }

    async Task<RecoveryFailures> Rehydrate(IEnumerable<IJobsManagerKeys> providers, HashSet<JobsManagerKey> managerKeys, CancellationToken cancellationToken)
    {
        var failures = new RecoveryFailures();
        var recovered = new HashSet<JobsManagerKey>();
        await RehydrateManagers(managerKeys, recovered, failures, cancellationToken);
        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JobsManagerKey[] discovered;
            try
            {
                discovered = (await provider.GetKeys(cancellationToken).WaitAsync(cancellationToken)).ToArray();
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.KeyProviderFailed(exception, provider.GetType());
                failures.Providers.Add(provider);
                continue;
            }
            await RehydrateManagers(discovered, recovered, failures, cancellationToken);
        }
        logger.RecoveredManagers(recovered.Count);
        return failures;
    }

    async Task RehydrateManagers(IEnumerable<JobsManagerKey> managerKeys, HashSet<JobsManagerKey> recovered, RecoveryFailures failures, CancellationToken cancellationToken)
    {
        foreach (var key in managerKeys.Where(recovered.Add))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await grainFactory.GetJobsManager(key.Scope, key.Namespace).Rehydrate().WaitAsync(cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.ManagerRecoveryFailed(exception, key);
                failures.Keys.Add(key);
            }
        }
    }

    sealed class RecoveryFailures
    {
        public HashSet<IJobsManagerKeys> Providers { get; } = [];
        public HashSet<JobsManagerKey> Keys { get; } = [];
        public bool HasFailures => Providers.Count > 0 || Keys.Count > 0;
    }
}
