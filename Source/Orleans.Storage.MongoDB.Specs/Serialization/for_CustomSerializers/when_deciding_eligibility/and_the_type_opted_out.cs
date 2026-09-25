// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.MongoDB.Serialization.for_CustomSerializers.given;

namespace Cratis.Orleans.Storage.MongoDB.Serialization.for_CustomSerializers.when_deciding_eligibility;

public class and_the_type_opted_out : Specification
{
    bool _result;

    void Because() => _result = CustomSerializers.IsEligibleForAutoRegistration(types_to_consider.OptedOut, new CustomSerializersOptions().AssemblyNameFragments);

    [Fact] void should_be_skipped() => _result.ShouldBeFalse();
}
