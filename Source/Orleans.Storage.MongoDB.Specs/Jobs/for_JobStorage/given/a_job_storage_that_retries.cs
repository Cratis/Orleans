// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.MongoDB.for_TransientRetry.given;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

public class a_job_storage_that_retries : a_job_storage
{
    protected RecordingTimeProvider _time;
    protected int _reads;
    protected Catch<JobState, JobError> _result;

    void Establish()
    {
        _time = new();
        _storage = new JobStorage(
            _database,
            JobTypes,
            NullLogger<JobStorage>.Instance,
            new TransientRetry(3, TimeSpan.FromMilliseconds(100), _time, jitter: () => 1d));
    }

    protected void FailReadsWith(Func<Exception> failure, int failures)
    {
        _collection.FindAsync(
            Arg.Any<FilterDefinition<JobState>>(),
            Arg.Any<FindOptions<JobState, JobState>>(),
            Arg.Any<CancellationToken>()).Returns(_ =>
            {
                _reads++;
                if (_reads <= failures)
                {
                    return Task.FromException<IAsyncCursor<JobState>>(failure());
                }

                var cursor = Substitute.For<IAsyncCursor<JobState>>();
                var moved = false;
                cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ => !moved && (moved = true));
                cursor.Current.Returns(_ => [_firstJob]);
                return Task.FromResult(cursor);
            });
    }
}
