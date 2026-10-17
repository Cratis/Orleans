// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Orleans.Storage.Sql.for_JobStepStorage.when_counting_by_status;

public class and_a_job_has_no_steps : job_steps_in_storage
{
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    async Task Because() => _result = await _storage.CountByStatus(_jobId);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_counts() => _result.AsT0.Count.ShouldEqual(0);
}
