// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Orleans.Storage.Sql.Jobs;

namespace Cratis.Orleans.Jobs.Integration.for_SqlJobStorage.when_reading_jobs_excluding_finalized_stops;

public class from_postgresql_with_many_finalized_stops : given.a_postgresql_jobs_database
{
    Catch<IImmutableList<JobState>> _before;
    Catch<IImmutableList<JobState>> _after;
    Job[] _expected = [];
    Job _removal = null!;

    Task Establish()
    {
        _removal = FinalizedRemoval();
        _expected = Enumerable.Range(0, 10).Select(_ => Stop(false))
            .Concat(Enumerable.Range(0, 5).Select(_ => Stop(null)))
            .Concat(Enumerable.Range(0, 5).Select(_ => Running())).Append(_removal).ToArray();
        return WithJobs(Enumerable.Range(0, 1000).Select(_ => Stop(true)).Concat(_expected).ToArray());
    }

    async Task Because()
    {
        var watch = Stopwatch.StartNew();
        _before = await _storage.GetJobs(RehydratedStatuses);
        var beforeMs = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        _after = await _storage.GetJobsExcludingFinalizedStops(RehydratedStatuses);
        watch.Stop();
        TestContext.Current.TestOutputHelper?.WriteLine($"PostgreSQL rows before={(_before.IsSuccess ? _before.AsT0.Count : -1)} after={(_after.IsSuccess ? _after.AsT0.Count : -1)}; milliseconds before={beforeMs:F3} after={watch.Elapsed.TotalMilliseconds:F3}");
    }

    [Fact] void should_succeed() => _after.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_only_jobs_that_need_rehydration() => _after.AsT0.Select(job => job.Id.Value).ShouldContainOnly(_expected.Select(job => job.Id));
    [Fact] void should_keep_the_finalized_removal() => _after.AsT0.Select(job => job.Id.Value).ShouldContain(_removal.Id);
    [Fact] void should_filter_in_the_query() => _commands.Exists(command => command.Contains("stopFinalized")).ShouldBeTrue();
    [Fact] void should_have_read_every_stop_before() => _before.AsT0.Count.ShouldEqual(1021);
}
