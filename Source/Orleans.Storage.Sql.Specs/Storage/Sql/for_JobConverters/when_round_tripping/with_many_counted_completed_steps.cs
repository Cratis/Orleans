// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class with_many_counted_completed_steps : for_JobStorage.given.jobs_in_storage
{
    int _baselineBytes;
    int _bytes;
    JobState _result;

    void Establish()
    {
        _baselineBytes = Encoding.UTF8.GetByteCount(ToEntity(_firstJob).StateJson);
        _firstJob.CompletedStepsCounted = Enumerable.Range(0, 100_000).Select(_ => JobStepId.New()).ToList();
    }

    void Because()
    {
        var entity = ToEntity(_firstJob);
        _bytes = Encoding.UTF8.GetByteCount(entity.StateJson);
        _result = entity.ToJobState(_serializerOptions);
    }

    [Fact] void should_read_every_counted_step() => _result.CompletedStepsCounted.Count.ShouldEqual(100_000);
    [Fact] void should_use_the_measured_json_bytes_for_entries() => (_bytes - _baselineBytes).ShouldEqual(3_899_999);
}
