// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_DatabaseNames;

public class when_the_name_contains_an_illegal_character : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => DatabaseNames.ForJobs("my scope", string.Empty));

    [Fact] void should_throw_invalid_database_name() => _error.ShouldBeOfExactType<InvalidDatabaseName>();
}
