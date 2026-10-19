// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class with_counted_stopped_steps : for_JobStorage.given.jobs_in_storage
{
    JobStepId _stepId;
    SqlJob _entity;
    JobState _result;

    void Establish()
    {
        _stepId = JobStepId.New();
        _firstJob.StoppedStepsCounted = [_stepId];
    }

    void Because()
    {
        _entity = ToEntity(_firstJob);
        _result = _entity.ToJobState(_serializerOptions);
    }

    [Fact] void should_write_the_counted_steps() => _entity.StateJson.ShouldContain("\"stoppedStepsCounted\":[");
    [Fact] void should_read_the_counted_steps() => _result.StoppedStepsCounted.ShouldContainOnly(_stepId);
}
