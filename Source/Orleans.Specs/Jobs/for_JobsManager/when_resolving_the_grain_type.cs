// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;

namespace Cratis.Orleans.Jobs.for_JobsManager;

/// <summary>
/// Orleans derives a grain type name from the class name alone, ignoring the namespace, so a grain that does not
/// name itself claims a name every other assembly's same-named grain also claims. Chronicle ships its own
/// <c language="csharp">JobsManager</c> and <c language="csharp">NullJob</c>, and a silo referencing both refuses to
/// start - "An entry with the key jobsmanager is already present". An application cannot work around that: it
/// references neither class directly and cannot rename either.
/// <para>
/// This covers every grain in the assembly rather than the two that were found, because the next grain added here
/// would reintroduce the same failure and nothing in a normal build or spec run starts a silo. Orleans' own
/// generated proxies are excluded - they are not grain implementations and never carry the attribute.
/// </para>
/// </summary>
public class when_resolving_the_grain_type : Specification
{
    IEnumerable<Type> _grains;

    void Because() => _grains = typeof(JobsManager).Assembly
        .GetTypes()
        .Where(type =>
            type is { IsClass: true, IsAbstract: false } &&
            typeof(IGrain).IsAssignableFrom(type) &&
            !type.IsDefined(typeof(GeneratedCodeAttribute), false));

    [Fact] void should_find_the_grains_it_is_meant_to_be_guarding() => _grains.ShouldNotBeEmpty();

    [Fact] void should_have_every_grain_name_itself_explicitly() => _grains
        .Where(grain => !grain.GetCustomAttributesData().Any(_ => _.AttributeType.Name == "GrainTypeAttribute"))
        .ShouldBeEmpty();
}
