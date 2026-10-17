// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_inspecting_concurrency;

/// <summary>
/// Pins the job grain's turn order. Chronicle decision 0011 relies on it: a job grain is not reentrant and none of
/// its grain methods interleave, and each job has a single activation, so a stop serializes with a start instead of
/// running inside it or beside it. Adding [Reentrant], [MayInterleave], [StatelessWorker], [AlwaysInterleave], [ReadOnly]
/// or [OneWay] to the job grain or its grain interfaces breaks that.
/// </summary>
public class and_it_is_the_job_grain : Specification
{
    Type[] _grainInterfaces;
    string[] _markings;

    void Establish() => _grainInterfaces = [.. given.GrainInterleaving.GrainInterfacesOf(typeof(Job<,>))];

    void Because() => _markings = [.. given.GrainInterleaving.MarkingsOn(typeof(Job<,>), _grainInterfaces)];

    [Fact] void should_inspect_the_job_interface() => _grainInterfaces.Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IJob<>)).ShouldBeTrue();
    [Fact] void should_inspect_the_untyped_job_interface() => _grainInterfaces.ShouldContain(typeof(IJob));
    [Fact] void should_inspect_the_drain_interface() => _grainInterfaces.ShouldContain(typeof(IJobDrain));
    [Fact] void should_not_let_any_call_interleave() => _markings.ShouldBeEmpty();
}
