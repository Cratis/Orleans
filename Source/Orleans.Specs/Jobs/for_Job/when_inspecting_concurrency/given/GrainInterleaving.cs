// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs.for_Job.when_inspecting_concurrency.given;

/// <summary>
/// Finds the Orleans markings that let calls to a grain interleave, skip its turn order or reach more than one activation.
/// </summary>
public static class GrainInterleaving
{
    static readonly Type[] _classMarkings = [typeof(ReentrantAttribute), typeof(MayInterleaveAttribute), typeof(StatelessWorkerAttribute)];
    static readonly Type[] _methodMarkings = [typeof(AlwaysInterleaveAttribute), typeof(ReadOnlyAttribute), typeof(OneWayAttribute)];

    /// <summary>
    /// Gets the grain interfaces a grain class implements.
    /// </summary>
    /// <param name="grainClass">The grain class.</param>
    /// <returns>The grain interfaces.</returns>
    public static IEnumerable<Type> GrainInterfacesOf(Type grainClass) =>
        grainClass.GetInterfaces().Where(_ => typeof(IAddressable).IsAssignableFrom(_) && _ != typeof(IAddressable));

    /// <summary>
    /// Describes every marking on the grain class and on the methods of the given grain interfaces that lets calls interleave.
    /// </summary>
    /// <param name="grainClass">The grain class.</param>
    /// <param name="grainInterfaces">The grain interfaces to inspect.</param>
    /// <returns>One description per marking found.</returns>
    public static IEnumerable<string> MarkingsOn(Type grainClass, IEnumerable<Type> grainInterfaces) =>
    [
        .. MarkedWith(grainClass, _classMarkings).Select(_ => $"{grainClass.Name} is [{_}]"),
        .. grainInterfaces.SelectMany(grainInterface =>
            MarkedWith(grainInterface, _classMarkings).Select(_ => $"{grainInterface.Name} is [{_}]")
                .Concat(grainInterface.GetMethods().SelectMany(method => MarkedWith(method, _methodMarkings).Select(_ => $"{grainInterface.Name}.{method.Name} is [{_}]"))))
    ];

    static IEnumerable<string> MarkedWith(MemberInfo member, Type[] markings) =>
        member.GetCustomAttributes(inherit: true).Select(_ => _.GetType()).Where(_ => markings.Any(marking => marking.IsAssignableFrom(_))).Select(_ => _.Name);
}
