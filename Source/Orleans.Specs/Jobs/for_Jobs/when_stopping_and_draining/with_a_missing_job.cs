// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping_and_draining;

public class with_a_missing_job : given.jobs
{
    DrainJobResult _result;

    void Establish()
    {
        _storage.GetJobType(_id).Returns(Task.FromResult<Cratis.Monads.Catch<JobType, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound));
        _factory.ClearReceivedCalls();
    }

    async Task Because() => _result = await _jobs.StopAndDrain(_key, _id, TimeSpan.Zero);

    [Fact] void should_report_not_found() => _result.ShouldEqual(new DrainJobResult.NotFound(_id));
    [Fact] void should_not_resolve_a_grain() => _factory.ReceivedCalls().ShouldBeEmpty();
}
