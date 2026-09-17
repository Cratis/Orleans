// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_DatabaseNames;

public class when_getting_database_name_for_a_namespace : Specification
{
    string _result;

    void Because() => _result = DatabaseNames.ForJobs("my-scope", "tenant-42");

    [Fact] void should_include_the_namespace() => _result.ShouldEqual("my-scope+jobs+tenant-42");
}
