// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Orleans.Jobs.for_JobsManager;

/// <summary>
/// Orleans derives a grain type name from the class name alone, so every <c language="csharp">JobsManager</c> in every referenced
/// assembly claims the same <c language="csharp">jobsmanager</c>. Chronicle ships one too, and a silo referencing both refuses to
/// start - "An entry with the key jobsmanager is already present" - which an application cannot work around,
/// because it references neither class directly and cannot rename either.
/// </summary>
public class when_resolving_the_grain_type : Specification
{
    CustomAttributeData _attribute;

    void Because() => _attribute = typeof(JobsManager)
        .GetCustomAttributesData()
        .SingleOrDefault(_ => _.AttributeType.Name == "GrainTypeAttribute");

    [Fact] void should_name_the_grain_type_explicitly() => _attribute.ShouldNotBeNull();
    [Fact] void should_not_leave_it_to_the_name_derived_from_the_class() => _attribute.ConstructorArguments[0].Value.ShouldNotEqual("jobsmanager");
}
