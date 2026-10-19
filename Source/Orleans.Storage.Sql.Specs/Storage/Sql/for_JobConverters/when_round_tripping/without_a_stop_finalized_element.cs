// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class without_a_stop_finalized_element : for_JobStorage.given.jobs_in_storage
{
    SqlJob _entity;
    JobState _result;

    void Establish()
    {
        // State written by a release that does not know about finalized stops.
        _entity = ToEntity(_firstJob);
        _entity.StateJson = _entity.StateJson.Replace("\"stopFinalized\":false,", string.Empty);
    }

    void Because() => _result = _entity.ToJobState(_serializerOptions);

    [Fact] void should_be_state_without_the_element() => _entity.StateJson.ShouldNotContain("stopFinalized");
    [Fact] void should_read_the_stop_as_not_finalized() => _result.StopFinalized.ShouldBeFalse();
    [Fact] void should_read_the_request() => _result.Request.ShouldEqual(_firstJob.Request);
}
