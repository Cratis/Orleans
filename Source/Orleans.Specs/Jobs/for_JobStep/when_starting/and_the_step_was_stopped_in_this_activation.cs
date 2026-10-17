// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class and_the_step_was_stopped_in_this_activation : given.a_job_step_with_a_stored_parent
{
    Result<StartJobStepError> _result;

    async Task Establish()
    {
        var stopped = await _jobStep.Stop(false);
        stopped.IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because() => _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));

    [Fact] void should_start_the_step() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_perform_the_step() => await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    [Fact]
    async Task should_supply_a_fresh_uncancelled_token()
    {
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.PerformToken.IsCancellationRequested.ShouldBeFalse();
    }

    void Destroy() => _jobStep.Dispose();
}
