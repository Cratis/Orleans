// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_observing_jobs;

public class with_a_malformed_registered_job : given.jobs_in_storage
{
    Exception _error;

    Task Establish()
    {
        var malformed = ToEntity(_firstJob);
        malformed.StateJson = "{\"type\":\"custom-job-name\",\"request\":42}";
        return WithJobs(_unknownJob, malformed);
    }

    async Task Because()
    {
        var completion = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _storage.ObserveJobs().AsT0.Subscribe(_ => { }, error => completion.TrySetResult(error));
        _error = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_report_the_deserialization_failure() => _error.ShouldBeOfExactType<JsonException>();
}
