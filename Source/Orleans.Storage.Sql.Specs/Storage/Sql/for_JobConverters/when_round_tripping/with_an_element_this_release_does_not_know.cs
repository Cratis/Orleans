// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class with_an_element_this_release_does_not_know : for_JobStorage.given.jobs_in_storage
{
    SqlJob _entity;
    JobState _result;

    void Establish()
    {
        // How a release without drains reads state written by one with them.
        _entity = ToEntity(_firstJob);
        _entity.StateJson = "{\"elementOfALaterRelease\":true," + _entity.StateJson[1..];
    }

    void Because() => _result = _entity.ToJobState(_serializerOptions);

    [Fact] void should_read_the_status() => _result.Status.ShouldEqual(_firstJob.Status);
    [Fact] void should_read_the_request() => _result.Request.ShouldEqual(_firstJob.Request);
}
