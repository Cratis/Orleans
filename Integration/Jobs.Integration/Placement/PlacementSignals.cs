// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Orleans.Jobs.Integration.Placement;

public sealed class PlacementSignals(string database, IReadOnlyDictionary<string, SiloPlan> plans, IEnumerable<SiloPlan>? extras = null, bool roundRobinWorkers = false)
{
    readonly ConcurrentDictionary<string, SiloPlan> _assigned = new();
    readonly ConcurrentQueue<SiloPlan> _extras = new(extras ?? []);
    readonly ConcurrentDictionary<SiloAddress, TaskCompletionSource> _gates = new();
    readonly ConcurrentDictionary<SiloAddress, bool> _dead = new();
    readonly ConcurrentDictionary<SiloAddress, bool> _released = new();
    readonly Lock _lock = new();
    readonly Dictionary<SiloAddress, int> _active = [];
    readonly Dictionary<SiloAddress, int> _peak = [];
    bool _allReleased;
    int _roundRobin = -1;

    public int ControllerDirectorCalls;
    public int WorkerDirectorCalls;

    public string Database { get; } = database;
    public JobsManagerKey Key { get; } = new("placement-store", "tenant-129");
    public bool RoundRobinWorkers { get; } = roundRobinWorkers;
    public ConcurrentQueue<(SiloAddress Silo, JobStepId Step)> Entries { get; } = new();
    public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SiloPlan PlanFor(string siloName) =>
        _assigned.GetOrAdd(
            siloName,
            static (name, state) =>
            {
                if (state.Plans.TryGetValue(name, out var plan))
                {
                    return plan;
                }
                if (state.Extras.TryDequeue(out var extra))
                {
                    return extra;
                }
                throw new InvalidOperationException($"No plan for silo '{name}'.");
            },
            (Plans: plans, Extras: _extras));

    public int NextRoundRobin() => Interlocked.Increment(ref _roundRobin);

    public void Entered(SiloAddress silo, JobStepId step)
    {
        lock (_lock)
        {
            var active = _active.GetValueOrDefault(silo) + 1;
            _active[silo] = active;
            _peak[silo] = Math.Max(_peak.GetValueOrDefault(silo), active);
        }
        Entries.Enqueue((silo, step));
    }

    public void Left(SiloAddress silo)
    {
        lock (_lock)
        {
            _active[silo] = _active.GetValueOrDefault(silo) - 1;
        }
    }

    public int ActiveOn(SiloAddress silo)
    {
        lock (_lock)
        {
            return _active.GetValueOrDefault(silo);
        }
    }

    public int PeakOn(SiloAddress silo)
    {
        lock (_lock)
        {
            return _peak.GetValueOrDefault(silo);
        }
    }

    public IReadOnlyList<SiloAddress> EntriesOn() => Entries.Select(entry => entry.Silo).ToList();

    public TaskCompletionSource GateOf(SiloAddress silo)
    {
        var gate = _gates.GetOrAdd(silo, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
        if (_dead.ContainsKey(silo))
        {
            return gate;
        }
        if (_allReleased || _released.ContainsKey(silo))
        {
            gate.TrySetResult();
        }
        return gate;
    }

    public void ReleaseSilo(SiloAddress silo)
    {
        _released[silo] = true;
        GateOf(silo);
    }

    public void ReleaseAll()
    {
        _allReleased = true;
        foreach (var silo in _gates.Keys)
        {
            GateOf(silo);
        }
    }

    public void MarkDead(SiloAddress silo) => _dead[silo] = true;
}
