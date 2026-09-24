// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Sql.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Storage.Sql.for_SqlJobsStorage;

/// <summary>
/// Specifies that resolving storage provisions the schema it is about to be used against.
/// </summary>
/// <remarks>
/// This is the spec the provider was missing. Without it, the package shipped a context whose tables
/// nothing created, and every read and write against a fresh database failed on a missing relation -
/// a failure that only ever surfaced downstream, in a consumer's integration suite.
/// </remarks>
public class when_getting_storage_for_a_scope_and_namespace : a_sqlite_jobs_database
{
    SqlJobsStorage _storage;
    JobsStorage _result;

    void Establish()
    {
        var options = new SqlJobsStorageOptions { OptionsResolver = (_, _) => _contextOptions };
        _storage = new SqlJobsStorage(
            Substitute.For<IJobTypes>(),
            Options.Create(options),
            Substitute.For<IServiceProvider>());
    }

    void Because() => _result = _storage.GetFor("some-scope", "some-namespace");

    [Fact] void should_return_storage() => _result.ShouldNotBeNull();

    [Fact] async Task should_create_the_jobs_table() => (await TableExists(WellKnownTableNames.Jobs)).ShouldBeTrue();

    [Fact] async Task should_create_the_job_steps_table() => (await TableExists(WellKnownTableNames.JobSteps)).ShouldBeTrue();

    [Fact] async Task should_be_able_to_query_jobs() => (await QuerySucceeds(context => context.Jobs.CountAsync())).ShouldBeTrue();

    [Fact] async Task should_be_able_to_query_job_steps() => (await QuerySucceeds(context => context.JobSteps.CountAsync())).ShouldBeTrue();

    async Task<bool> TableExists(string tableName)
    {
        await using var context = CreateContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }

    async Task<bool> QuerySucceeds(Func<Jobs.JobsDbContext, Task<int>> query)
    {
        try
        {
            await using var context = CreateContext();
            await query(context);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
