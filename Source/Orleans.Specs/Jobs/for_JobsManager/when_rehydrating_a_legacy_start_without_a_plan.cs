// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_a_legacy_start_without_a_plan : given.the_manager
{
    readonly ILogger<JobsManager> _logger = Substitute.For<ILogger<JobsManager>>();
    Mock<INullJobWithSomeRequest> _job;

    protected override ILogger<JobsManager> CreateLogger() => _logger;

    void Establish()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _job = AddJob<INullJobWithSomeRequest>(JobId.New());
        _storedJobs.Single().Status = JobStatus.StartingSteps;
        _storedJobs.Single().Created = DateTimeOffset.UtcNow.AddHours(-2);
        _job.Setup(job => job.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Failed(CannotResumeJobError.MissingStepPlan));
    }

    async Task Because()
    {
        await _manager.Rehydrate();
        await WaitForRehydration();
    }

    [Fact] void should_log_the_specific_reason() => _logger.ReceivedCalls().Any(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        call.GetArguments()[2]!.ToString()!.Contains("without a saved step plan", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_delete_the_legacy_starting_job() => _job.Verify(job => job.Remove(), Times.Never);
    [Fact] void should_preserve_the_status() => _storedJobs.Single().Status.ShouldEqual(JobStatus.StartingSteps);
}
