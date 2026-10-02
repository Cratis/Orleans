// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_has_more_steps_than_the_start_limit : given.the_job
{
    const int StepCount = 10;
    const int Limit = 2;

    int _inFlight;
    int _peak;
    readonly List<Mock<given.ISomeJobStep>> _steps = [];
    Result<ResumeJobSuccess, ResumeJobError> _result;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = Limit };

    void Establish()
    {
        _job.ShouldBeResumable = true;
        for (var i = 0; i < StepCount; i++)
        {
            var step = AddJobStep(Guid.NewGuid());
            step.Setup(_ => _.Start(It.IsAny<GrainId>())).Returns(async () =>
            {
                var now = Interlocked.Increment(ref _inFlight);
                int seen;
                while (now > (seen = Volatile.Read(ref _peak)) && Interlocked.CompareExchange(ref _peak, now, seen) != seen)
                {
                }

                await Task.Delay(20);
                Interlocked.Decrement(ref _inFlight);
                return Result<StartJobStepError>.Success();
            });
            _steps.Add(step);
        }
    }

    async Task Because()
    {
        await _job.Start(new());
        _peak = 0;
        foreach (var step in _steps)
        {
            step.Invocations.Clear();
        }

        _job.CurrentState.Status = JobStatus.Stopped;
        _result = await _job.Resume();
    }

    [Fact] void should_resume_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_never_start_more_steps_than_the_limit() => (_peak <= Limit).ShouldBeTrue();
    [Fact] void should_start_every_step_again() => _steps.ForEach(step => step.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once));
}
