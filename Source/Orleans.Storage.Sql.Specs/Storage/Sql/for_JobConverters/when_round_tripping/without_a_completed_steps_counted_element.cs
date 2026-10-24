// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class without_a_completed_steps_counted_element : for_JobStorage.given.jobs_in_storage
{
    SqlJob _entity;
    JobState _result;

    void Establish()
    {
        _entity = ToEntity(_firstJob);
        _entity.StateJson = _entity.StateJson.Replace("\"completedStepsCounted\":[],", string.Empty);
    }

    void Because() => _result = _entity.ToJobState(_serializerOptions);

    [Fact] void should_be_state_without_the_element() => _entity.StateJson.ShouldNotContain("completedStepsCounted");
    [Fact] void should_read_no_counted_steps() => _result.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_read_the_status() => _result.Status.ShouldEqual(_firstJob.Status);
    [Fact] void should_read_the_request() => _result.Request.ShouldEqual(_firstJob.Request);
}
