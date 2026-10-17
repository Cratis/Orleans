// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;
using Orleans.Serialization.Invocation;

namespace Cratis.Orleans.Jobs.for_Job.when_inspecting_concurrency;

/// <summary>
/// Proves the inspection the job grain's turn-order spec relies on still finds every marking it looks for.
/// </summary>
public class and_a_grain_is_marked_to_interleave : Specification
{
    string[] _markings;

    void Because() => _markings = [.. given.GrainInterleaving.MarkingsOn(typeof(InterleavingGrain), given.GrainInterleaving.GrainInterfacesOf(typeof(InterleavingGrain)))];

    [Fact] void should_find_every_marking() => _markings.Length.ShouldEqual(7);
    [Fact] void should_find_a_reentrant_grain() => _markings.ShouldContain("InterleavingGrain is [ReentrantAttribute]");
    [Fact] void should_find_a_stateless_worker() => _markings.ShouldContain("InterleavingGrain is [StatelessWorkerAttribute]");
    [Fact] void should_find_a_grain_that_may_interleave() => _markings.ShouldContain("InterleavingGrain is [MayInterleaveAttribute]");
    [Fact] void should_find_a_method_that_always_interleaves() => _markings.ShouldContain("IInterleavingGrain.Interleaving is [AlwaysInterleaveAttribute]");
    [Fact] void should_find_a_read_only_method() => _markings.ShouldContain("IInterleavingGrain.Reading is [ReadOnlyAttribute]");
    [Fact] void should_find_a_one_way_method() => _markings.ShouldContain("IInterleavingGrain.Notifying is [OneWayAttribute]");
    [Fact] void should_find_a_marking_on_an_inherited_grain_interface() => _markings.ShouldContain("IInterleavingBase.Interleaving is [AlwaysInterleaveAttribute]");

    public interface IInterleavingBase : IGrainWithGuidKey
    {
        [AlwaysInterleave]
        Task Interleaving();
    }

    public interface IInterleavingGrain : IInterleavingBase
    {
        [AlwaysInterleave]
        new Task Interleaving();

        [ReadOnly]
        Task Reading();

        [OneWay]
        Task Notifying();
    }

    [Reentrant]
    [MayInterleave(nameof(MayInterleave))]
    [StatelessWorker]
    public class InterleavingGrain : IInterleavingGrain
    {
        public static bool MayInterleave(IInvokable request) => false;

        public Task Interleaving() => Task.CompletedTask;

        public Task Reading() => Task.CompletedTask;

        public Task Notifying() => Task.CompletedTask;
    }
}
