// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    Task? _recovery;

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(JobsRecovery), ServiceLifecycleStage.Active + 1, Start, Stop);

    /// <inheritdoc/>
    public void Dispose() => _stopping.Dispose();

    Task Start(CancellationToken cancellationToken)
    {
        // Grain calls are made only after Active, off the lifecycle turn. A backlog never blocks startup.
        _recovery = Task.Run(() => RecoverAndWatch(_stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync();
        if (_recovery is not null)
        {
            await _recovery.WaitAsync(cancellationToken);
        }
    }

    async Task RecoverAndWatch(CancellationToken cancellationToken)
    {
        try
        {
            var hasHostProvider = await Rehydrate(cancellationToken);
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
                    hasHostProvider = await Rehydrate(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown cancels discovery and grain-call waits; durable jobs remain available for recovery.
        }
        catch (Exception exception)
        {
            logger.RecoveryFailed(exception);
        }
    }

    bool IsElected(ClusterMembershipSnapshot snapshot) => snapshot.Members.Values
        .Where(member => member.Status is SiloStatus.Active)
        .OrderBy(member => member.SiloAddress.Generation)
        .ThenBy(member => member.SiloAddress.ToParsableString(), StringComparer.Ordinal)
        .FirstOrDefault()?.SiloAddress.Equals(localSilo.SiloAddress) == true;

    async Task<bool> Rehydrate(CancellationToken cancellationToken)
    {
        var providers = keys.ToArray();
        var hasHostProvider = providers.Any(provider => provider is not JobsManagerKeys);
        if (!hasHostProvider)
        {
            logger.IncompleteRecoveryKeys();
        }
        var recovered = new HashSet<JobsManagerKey>();
        foreach (var provider in providers)
        {
            JobsManagerKey[] managerKeys;
            try
            {
                managerKeys = (await provider.GetKeys(cancellationToken).WaitAsync(cancellationToken)).ToArray();
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.KeyProviderFailed(exception, provider.GetType());
                continue;
            }
            foreach (var key in managerKeys.Where(recovered.Add))
            {
                try
                {
                    await grainFactory.GetJobsManager(key.Scope, key.Namespace).Rehydrate().WaitAsync(cancellationToken);
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.ManagerRecoveryFailed(exception, key);
                }
            }
        }
        logger.RecoveredManagers(recovered.Count);
        return hasHostProvider;
    }
}
