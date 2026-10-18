// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_round_tripping;

public class with_a_step_plan : given.jobs_in_storage
{
    PlannedJobStep _planned;
    JobState _result;

    async Task Establish()
    {
        _planned = new(JobStepId.New(), typeof(IJob), 10, typeof(KnownRequest).AssemblyQualifiedName!, "{\"message\":\"first\"}");
        _firstJob.StepPlan = [_planned];
        (await _storage.Save(_firstJob.Id, _firstJob)).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _result = (await _storage.Read<JobState>(_firstJob.Id)).AsT0;

    [Fact] void should_preserve_the_identity_stage_type_and_request() => _result.StepPlan!.Single().ShouldEqual(_planned);
}
