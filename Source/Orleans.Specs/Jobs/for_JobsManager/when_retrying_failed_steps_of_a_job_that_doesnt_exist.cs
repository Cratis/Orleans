// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_retrying_failed_steps_of_a_job_that_doesnt_exist : given.the_manager
{
    Result<int, RetryFailedStepsError> _result;

    async Task Because() => _result = await _manager.RetryFailedSteps(Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024"));

    [Fact] void should_answer_unknown() => _result.Match(_ => RetryFailedStepsError.NotSupported, error => error).ShouldEqual(RetryFailedStepsError.Unknown);
}
