// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class with_a_pending_drain : for_JobStorage.given.jobs_in_storage
{
    SqlJob _entity;
    JobState _result;

    void Establish() => _firstJob.DrainPending = true;

    void Because()
    {
        _entity = ToEntity(_firstJob);
        _result = _entity.ToJobState(_serializerOptions);
    }

    [Fact] void should_write_the_pending_drain() => _entity.StateJson.ShouldContain("\"drainPending\":true");
    [Fact] void should_read_the_pending_drain() => _result.DrainPending.ShouldBeTrue();
}
