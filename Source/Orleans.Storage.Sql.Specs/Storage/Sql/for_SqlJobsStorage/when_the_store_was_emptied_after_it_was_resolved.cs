// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Storage.Sql.for_SqlJobsStorage;

/// <summary>
/// Applying the migrations is part of resolving storage, and resolving is cached - so an application that
/// empties the database behind the job system gets a cache pointing at tables that are no longer there, and
/// nothing to make them again. Nothing fails at the moment the database is emptied; the next write is what
/// reports that there is no such table. Chronicle does exactly this between integration specs.
/// </summary>
public class when_the_store_was_emptied_after_it_was_resolved : given.a_sqlite_jobs_database
{
    SqlJobsStorage _storage;
    bool _tableExistsAfterReset;

    void Establish()
    {
        _storage = new SqlJobsStorage(
            Substitute.For<IJobTypes>(),
            Options.Create(new SqlJobsStorageOptions { OptionsResolver = (_, _) => _contextOptions }),
            new ServiceCollection().BuildServiceProvider());

        _storage.GetFor("the-scope", "the-namespace");

        // What emptying the store looks like: every table goes, migration history included. That is what
        // Chronicle's development-mode wipe does to a SQLite file between integration specs.
        using var context = CreateContext();
        context.Database.ExecuteSqlRaw($"DROP TABLE \"{WellKnownTableNames.Jobs}\"");
        context.Database.ExecuteSqlRaw($"DROP TABLE \"{WellKnownTableNames.JobSteps}\"");
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"__EFMigrationsHistory\"");
    }

    void Because()
    {
        _storage.Reset();
        _storage.GetFor("the-scope", "the-namespace");

        using var context = CreateContext();
        _tableExistsAfterReset = context.Database
            .SqlQuery<string>($"SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {WellKnownTableNames.Jobs}")
            .Any();
    }

    [Fact] void should_make_the_tables_again() => _tableExistsAfterReset.ShouldBeTrue();
}
