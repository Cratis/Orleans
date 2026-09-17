// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_DatabaseNames;

public class when_the_name_exceeds_the_mongodb_limit : Specification
{
    const string LongScope = "a-scope-name-that-is-far-too-long-to-be-legal-as-a-mongodb-database-name-because-it-exceeds-sixty-three-bytes-in-length";
    Exception? _error;

    void Because() => _error = Catch.Exception(() => DatabaseNames.ForJobs(LongScope, string.Empty));

    [Fact] void should_throw_invalid_database_name() => _error.ShouldBeOfExactType<InvalidDatabaseName>();
}
