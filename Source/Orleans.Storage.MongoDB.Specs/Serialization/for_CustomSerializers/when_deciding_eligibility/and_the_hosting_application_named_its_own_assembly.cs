// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.MongoDB.Serialization.for_CustomSerializers.given;

namespace Cratis.Orleans.Storage.MongoDB.Serialization.for_CustomSerializers.when_deciding_eligibility;

/// <summary>
/// An application keeping serializers of its own outside this package names its assembly, and they register.
/// Without this, they are found and silently skipped, and the driver reads those documents its own way - which
/// looks like nothing at all going wrong until a document written in an older shape is read back.
/// </summary>
public class and_the_hosting_application_named_its_own_assembly : Specification
{
    bool _result;

    void Because()
    {
        var options = new CustomSerializersOptions();
        options.AssemblyNameFragments.Add("MongoDB.Bson");
        _result = CustomSerializers.IsEligibleForAutoRegistration(types_to_consider.FromAnotherAssembly, options.AssemblyNameFragments);
    }

    [Fact] void should_be_registered() => _result.ShouldBeTrue();
}
