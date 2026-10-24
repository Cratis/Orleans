// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Jobs.Integration.for_SqlJobStorage.when_reading_jobs_excluding_finalized_stops;

public class from_sql_server_with_empty_or_invalid_state : given.a_sql_server_jobs_database
{
    Job[] _undecodable = [];
    Job _finalized = null!;
    List<Guid> _ids = [];
    Exception? _error;

    Task Establish()
    {
        _undecodable = new[] { "", "not json", "{\"stopFinalized\":", "[]", "{\"stopFinalized\":\"yes\"}" }
            .Select(state => Raw(JobStatus.Stopped, state)).ToArray();
        _finalized = Stop(true);
        return WithJobs(_undecodable.Append(_finalized).ToArray());
    }

    async Task Because()
    {
        _error = await Specifications.Catch.Exception(async () =>
        {
            await using var context = CreateContext();
            var predicate = FinalizedStopFilter.For(context, out var table);
            var sql = $"SELECT * FROM {table} WHERE {predicate}";
            _ids = await context.Jobs.FromSqlRaw(sql).Select(job => job.Id).ToListAsync();
        });
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_keep_every_undecodable_state() => _ids.ShouldContainOnly(_undecodable.Select(job => job.Id));
    [Fact] void should_exclude_the_finalized_stop() => _ids.ShouldNotContain(_finalized.Id);
}
